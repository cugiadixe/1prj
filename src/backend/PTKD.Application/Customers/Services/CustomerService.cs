using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Customers.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Customers.Services;

public class CustomerService : ICustomerService
{
    // Mã quyền dạng chuỗi (PermissionCodes nằm ở tầng PTKD.Api, không tham chiếu ngược được).
    private const string ViewBasicPermission = "CUSTOMER_VIEW_BASIC";
    private const string GraveViewPermission = "GRAVE_VIEW";
    private const string CreateFinalPermission = "CUSTOMER_CREATE_FINAL";
    private const string MasterUpdatePermission = "CUSTOMER_MASTER_UPDATE";
    // Trạng thái "đã mất" — đặt khi khách trở thành cốt trong mộ (xem GraveService). "Còn sống" là
    // mọi trạng thái khác.
    private const string DeceasedStatus = "DECEASED";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public CustomerService(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter,
        IPermissionEvaluator permissionEvaluator)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<CustomerDetailDto> CreateCustomerAsync(CreateCustomerRequest request, long actorUserId, CancellationToken ct = default)
    {
        // Khách gắn công ty nào thì người tạo phải có quyền tạo ở công ty đó. Khách CHƯA gắn công ty
        // (mồ côi) chỉ người toàn cục tạo được — nếu không sẽ đẻ ra khách chính người tạo cũng không
        // thấy lại được, và là lỗ để lách phạm vi.
        var createScope = await _permissionEvaluator.ResolveAsync(actorUserId, CreateFinalPermission, ct);
        if (request.InitialCompanyId.HasValue)
        {
            if (!createScope.Allows(request.InitialCompanyId.Value))
                throw new PermissionDeniedException("CUS_CREATE_FORBIDDEN_COMPANY",
                    "Bạn không có quyền tạo khách hàng ở công ty này.");
        }
        else if (!createScope.IsUnrestricted)
        {
            throw new PermissionDeniedException("CUS_CREATE_ORPHAN_FORBIDDEN",
                "Vui lòng chọn công ty cho khách hàng (chỉ quyền toàn cục mới tạo được khách chưa gắn công ty).");
        }

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (await context.Customers.AnyAsync(c => c.CustomerCode == request.CustomerCode, ct))
                throw new BusinessRuleValidationException("CUS_DUPLICATE_CUSTOMER_CODE", "Customer code already exists.");

            if (!string.IsNullOrWhiteSpace(request.Cccd))
            {
                var hasDuplicate = await context.Profiles
                    .AnyAsync(p => p.Cccd == request.Cccd && p.IsActive, ct);
                if (hasDuplicate)
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_CCCD", "An active customer with this CCCD already exists.");
            }

            // Trùng SĐT: CHẶN MỀM (khác CCCD khoá cứng) — SĐT có thể dùng chung trong gia đình. Nếu
            // người dùng đã xác nhận (ConfirmDuplicatePhone) thì cho tạo. Không có unique index cho phone.
            if (!request.ConfirmDuplicatePhone && !string.IsNullOrWhiteSpace(request.Phone))
            {
                var phoneDuplicate = await context.Profiles
                    .AnyAsync(p => p.Phone == request.Phone && p.IsActive, ct);
                if (phoneDuplicate)
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_PHONE", "An active customer with this phone number already exists.");
            }

            var profile = new Profile(
                request.FullName, request.Cccd, request.Dob, request.DobPartial, request.DobPrecision,
                request.Gender, request.PermanentAddress, request.CccdIssueDate, request.CccdIssuePlace,
                request.TaxCode, request.Phone, request.ContactAddress,
                request.DeathDateSolar, request.DeathDateLunar, request.DeathPlace, request.Hometown);
            profile.SetCreatedBy(actorUserId);
            context.Profiles.Add(profile);
            await context.SaveChangesAsync(ct);

            var customer = new Customer(request.CustomerCode, profile.Id);
            customer.SetCreatedBy(actorUserId);
            // Tạo khách ở tình trạng đã mất → đặt DECEASED ngay (không chờ quy trình gắn mộ). Ngày mất
            // đã lưu ở profile phía trên.
            if (request.IsDeceased)
                customer.SetStatus(DeceasedStatus, actorUserId);
            context.Customers.Add(customer);
            await context.SaveChangesAsync(ct);

            if (request.InitialCompanyId.HasValue)
            {
                if (!await context.Companies.AnyAsync(c => c.Id == request.InitialCompanyId.Value && c.IsActive, ct))
                    throw new EntityNotFoundException("CUS_COMPANY_NOT_FOUND", "Company not found or inactive.");

                var companyContext = new CustomerCompanyContext(
                    customer.Id, request.InitialCompanyId.Value, request.AssignedStaffId,
                    request.InternalNotes, DateTime.UtcNow);
                companyContext.SetCreatedBy(actorUserId);
                context.CustomerCompanyContexts.Add(companyContext);
                await context.SaveChangesAsync(ct);
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_CREATE",
                EntityType = "Customer",
                EntityId = customer.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { customer.CustomerCode, profile.FullName, profile.Cccd, customer.CustomerStatus, request.InitialCompanyId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToDetailDto(customer, profile, false);
        });
    }

    public async Task<CustomerDetailDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var customer = await context.Customers
                .Include(c => c.Profile)
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (customer == null)
                throw new EntityNotFoundException("CUS_CUSTOMER_NOT_FOUND", "Customer not found.");

            var updateScope = await _permissionEvaluator.ResolveAsync(actorUserId, MasterUpdatePermission, ct);
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, id, updateScope, "CUS_UPDATE_FORBIDDEN_COMPANY", ct);

            if (!customer.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The customer has been modified by another process.");

            if (!string.IsNullOrWhiteSpace(request.Cccd) && request.Cccd != customer.Profile.Cccd)
            {
                var hasDuplicate = await context.Profiles
                    .AnyAsync(p => p.Cccd == request.Cccd && p.IsActive && p.Id != customer.ProfileId, ct);
                if (hasDuplicate)
                    throw new BusinessRuleValidationException("CUS_DUPLICATE_CCCD", "An active customer with this CCCD already exists.");
            }

            var beforeState = JsonSerializer.Serialize(new
            {
                customer.Profile.FullName, customer.Profile.Cccd, customer.Profile.Phone,
                customer.Profile.PermanentAddress, customer.Profile.ContactAddress
            });

            customer.Profile.Update(
                request.FullName, request.Cccd, request.Dob, request.DobPartial, request.DobPrecision,
                request.Gender, request.PermanentAddress, request.CccdIssueDate, request.CccdIssuePlace,
                request.TaxCode, request.Phone, request.ContactAddress,
                request.DeathDateSolar, request.DeathDateLunar, request.DeathPlace, request.Hometown,
                actorUserId);

            customer.MarkUpdated(actorUserId);

            var afterState = JsonSerializer.Serialize(new
            {
                request.FullName, request.Cccd, request.Phone,
                request.PermanentAddress, request.ContactAddress
            });

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The customer has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_UPDATE",
                EntityType = "Customer",
                EntityId = customer.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Reason = request.Reason,
                BeforeStateJson = beforeState,
                AfterStateJson = afterState
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToDetailDto(customer, customer.Profile, false);
        });
    }

    public async Task<CustomerDetailDto?> GetCustomerByIdAsync(long id, bool canViewSensitive, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Khách thuộc công ty người gọi không có quyền -> coi như không thấy (404).
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewBasicPermission, ct);
        if (!await CustomerCompanyScope.CanAccessCustomerAsync(context, id, scope, ct))
            return null;

        var customer = await context.Customers
            .AsNoTracking()
            .Include(c => c.Profile)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (customer == null) return null;

        var dto = MapToDetailDto(customer, customer.Profile, !canViewSensitive);
        dto.Tags = await context.CustomerTags.AsNoTracking()
            .Where(x => x.CustomerId == id)
            .OrderBy(x => x.Tag!.Name)
            .Select(x => new PTKD.Application.Tags.DTOs.TagDto
            {
                Id = x.Tag!.Id, TagType = x.Tag.TagType, Name = x.Tag.Name,
                Color = x.Tag.Color, IsActive = x.Tag.IsActive
            })
            .ToArrayAsync(ct);
        return dto;
    }

    public async Task<CustomerOverviewDto?> GetCustomerOverviewAsync(long customerId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Phải thấy được khách trước (như GetById) — không thấy khách thì 404.
        var custScope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewBasicPermission, ct);
        if (!await CustomerCompanyScope.CanAccessCustomerAsync(context, customerId, custScope, ct))
            return null;

        // Dữ liệu MỘ theo phạm vi GRAVE_VIEW RIÊNG — KHÔNG dùng chung scope khách (footgun ServiceFiltered).
        var graveScope = await _permissionEvaluator.ResolveAsync(actorUserId, GraveViewPermission, ct);
        var result = new CustomerOverviewDto();
        if (!graveScope.Granted)
        {
            result.GraveAccessDenied = true;
            return result;
        }

        var scopedGraves = GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), graveScope);

        // Mộ khách SỞ HỮU (chủ mộ) — kèm nghĩa trang + số cốt đang an táng (chỉ suất ACTIVE).
        result.OwnedGraves = await scopedGraves
            .Where(g => g.OwnerCustomerId == customerId)
            .OrderBy(g => g.GraveCode)
            .Select(g => new OverviewGraveDto
            {
                GraveId = g.Id,
                GraveCode = g.GraveCode,
                CemeteryName = g.Cemetery!.Name,
                Zone = g.Zone,
                PlotNumber = g.PlotNumber,
                GraveType = g.GraveType,
                Status = g.Status,
                CotCount = g.CotCount,
                ActiveOccupantCount = g.Occupants.Count(o => o.Status == GraveOccupant.StatusActive),
            })
            .ToArrayAsync(ct);

        // Mộ khách ĐƯỢC AN TÁNG (là cốt) — qua Occupants.DeceasedCustomerId, cùng phạm vi mộ. Gồm cả
        // suất đã bốc (RELOCATED) để thấy lịch sử; FE phân biệt theo OccupantStatus.
        result.BuriedIn = await scopedGraves
            .SelectMany(
                g => g.Occupants.Where(o => o.DeceasedCustomerId == customerId),
                (g, o) => new BuriedInGraveDto
                {
                    GraveId = g.Id,
                    GraveCode = g.GraveCode,
                    CemeteryName = g.Cemetery!.Name,
                    Zone = g.Zone,
                    GraveStatus = g.Status,
                    OccupantStatus = o.Status,
                    BurialDate = o.BurialDate,
                    RelocatedAt = o.RelocatedAt,
                    DeceasedRelationship = o.DeceasedRelationship,
                    OwnerCustomerId = g.OwnerCustomerId,
                    OwnerName = g.Owner != null ? g.Owner.Profile!.FullName : null,
                })
            .ToArrayAsync(ct);

        return result;
    }

    public async Task<PagedResult<CustomerListItemDto>> SearchCustomersAsync(CustomerSearchRequest request, bool canViewSensitive, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Lọc theo công ty NGAY trong truy vấn (300K+ khách): chỉ khách gắn công ty người gọi được
        // cấp CUSTOMER_VIEW_BASIC. Áp trước mọi bộ lọc khác để count/paging đều tôn trọng phạm vi.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewBasicPermission, ct);
        var query = CustomerCompanyScope.ApplyScope(
            context.Customers.AsNoTracking().Include(c => c.Profile), context, scope);

        // Phạm vi MỘ của người xem — dùng cho cột "phần mộ sở hữu" và bộ lọc; không lộ mộ ngoài quyền.
        var graveScope = await _permissionEvaluator.ResolveAsync(actorUserId, GraveViewPermission, ct);

        if (!string.IsNullOrWhiteSpace(request.CustomerStatus))
            query = query.Where(c => c.CustomerStatus == request.CustomerStatus);

        // Lọc tình trạng sống/mất. "Đã mất" = CustomerStatus == DECEASED (đặt khi khách thành cốt
        // trong mộ). "Còn sống" = mọi trạng thái còn lại. Đọc cùng cột với bộ lọc CustomerStatus.
        var hasLifeFilter = !string.IsNullOrWhiteSpace(request.LifeStatus);
        if (hasLifeFilter)
        {
            if (string.Equals(request.LifeStatus, DeceasedStatus, StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.CustomerStatus == DeceasedStatus);
            else if (string.Equals(request.LifeStatus, "ALIVE", StringComparison.OrdinalIgnoreCase))
                query = query.Where(c => c.CustomerStatus != DeceasedStatus);
        }

        var hasSearch = !string.IsNullOrWhiteSpace(request.Search);
        if (hasSearch)
        {
            // Nhận diện ý định để TRÁNH LIKE '%x%' 4 cột (full scan ~2s trên 300K KH).
            //  • Mã KH (chữ + số, vd KH0098943) → prefix trên customer_code → SEEK index, ~tức thì.
            //  • Toàn số → SĐT/CCCD (contains, 1 cột).
            //  • Chữ → họ tên (contains, 1 cột).
            var term = request.Search!.Trim();
            var allDigits = term.Length > 0 && term.All(char.IsDigit);
            var looksLikeCode = !allDigits && term.Length >= 2 && char.IsLetter(term[0]) && term.Any(char.IsDigit);

            if (looksLikeCode)
            {
                // Tìm "chứa" trên đúng MỘT cột customer_code (không phải 4 cột) để "H0098948" vẫn
                // ra "KH0098948". .Contains → LIKE '%term%' (EF tự escape wildcard). Chấp nhận scan
                // 1 cột thay vì seek prefix, đổi lại tìm gần đúng theo yêu cầu.
                query = query.Where(c => c.CustomerCode.Contains(term));
            }
            else if (allDigits)
                query = query.Where(c =>
                    (c.Profile.Phone != null && c.Profile.Phone.Contains(term)) ||
                    (c.Profile.Cccd != null && c.Profile.Cccd.Contains(term)));
            else
                query = query.Where(c => c.Profile.FullName.Contains(term));
        }

        var hasContextFilter = request.CompanyId.HasValue || request.AssignedStaffId.HasValue || request.UnassignedStaff == true;
        if (hasContextFilter)
        {
            query = query.Where(c => context.CustomerCompanyContexts.Any(ctx =>
                ctx.CustomerId == c.Id
                && (request.CompanyId == null || ctx.CompanyId == request.CompanyId)
                && (request.AssignedStaffId == null || ctx.AssignedStaffId == request.AssignedStaffId)
                && (request.UnassignedStaff != true || ctx.AssignedStaffId == null)));
        }

        var hasTagFilter = request.TagIds != null && request.TagIds.Length > 0;
        if (hasTagFilter)
        {
            var tagIds = request.TagIds!;
            query = query.Where(c => context.CustomerTags.Any(x => x.CustomerId == c.Id && tagIds.Contains(x.TagId)));
        }

        // Lọc theo SỞ HỮU MỘ (trong phạm vi GRAVE_VIEW của người gọi). Inline mệnh đề công ty của mộ
        // cho chắc chắn dịch được SQL (mộ thuộc công ty qua Cemetery).
        var hasOwnsGraveFilter = request.OwnsGrave.HasValue;
        if (hasOwnsGraveFilter)
        {
            var wantOwner = request.OwnsGrave!.Value;
            if (!graveScope.Granted)
            {
                // Không có quyền xem mộ ⇒ theo phạm vi coi như không sở hữu mộ nào.
                if (wantOwner) query = query.Where(_ => false);
            }
            else if (graveScope.IsUnrestricted)
            {
                var excluded = graveScope.ExcludedCompanyIds;
                query = wantOwner
                    ? query.Where(c => context.Graves.Any(g => g.OwnerCustomerId == c.Id && !excluded.Contains(g.Cemetery!.CompanyId)))
                    : query.Where(c => !context.Graves.Any(g => g.OwnerCustomerId == c.Id && !excluded.Contains(g.Cemetery!.CompanyId)));
            }
            else
            {
                var allowed = graveScope.AllowedCompanyIds;
                query = wantOwner
                    ? query.Where(c => context.Graves.Any(g => g.OwnerCustomerId == c.Id && allowed.Contains(g.Cemetery!.CompanyId)))
                    : query.Where(c => !context.Graves.Any(g => g.OwnerCustomerId == c.Id && allowed.Contains(g.Cemetery!.CompanyId)));
            }
        }

        // Lọc "chưa có phần mộ": khách CHƯA là cốt đang an táng ở mộ nào (dùng cho ô chọn người thân
        // đã mất khi khai quan hệ). Người đã bốc/cải táng (suất RELOCATED) vẫn tính là chưa có mộ.
        var hasNotBuriedFilter = request.NotBuried == true;
        if (hasNotBuriedFilter)
            query = query.Where(c => !context.GraveOccupants.Any(o =>
                o.DeceasedCustomerId == c.Id && o.Status == GraveOccupant.StatusActive));

        var mask = !canViewSensitive;

        var projectedQuery = query
            .OrderBy(c => c.Id)
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                FullName = c.Profile.FullName,
                Cccd = mask ? MaskCccd(c.Profile.Cccd) : c.Profile.Cccd,
                Phone = mask ? MaskPhone(c.Profile.Phone) : c.Profile.Phone,
                CustomerStatus = c.CustomerStatus,
                IsDeceased = c.CustomerStatus == DeceasedStatus,
                // Companies nạp tách ở dưới (tra từ điển tên) — tránh subquery lồng khó dịch SQL.
                CreatedAt = c.CreatedAt,
                Tags = context.CustomerTags
                    .Where(x => x.CustomerId == c.Id)
                    .OrderBy(x => x.Tag!.Name)
                    .Select(x => new PTKD.Application.Tags.DTOs.TagDto
                    {
                        Id = x.Tag!.Id, TagType = x.Tag.TagType, Name = x.Tag.Name,
                        Color = x.Tag.Color, IsActive = x.Tag.IsActive
                    }).ToArray()
            });

        var anyFilter = hasSearch || hasContextFilter || hasTagFilter || hasLifeFilter || hasOwnsGraveFilter
            || hasNotBuriedFilter || !string.IsNullOrWhiteSpace(request.CustomerStatus);

        int totalCount;
        CustomerListItemDto[] items;

        if (!anyFilter)
        {
            // Đếm trên `query` (đã áp phạm vi công ty), KHÔNG phải toàn bảng — nếu không người
            // quyền-công-ty sẽ thấy tổng số của cả hệ dù danh sách đã bị lọc.
            totalCount = await query.CountAsync(ct);
            items = await projectedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToArrayAsync(ct);
        }
        else
        {
            totalCount = await query.CountAsync(ct);
            items = await projectedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToArrayAsync(ct);
        }

        // Nạp công ty phụ trách + nhân viên phụ trách cho ĐÚNG trang hiện tại (≤ pageSize khách).
        // Tách truy vấn + tra từ điển tên cho chắc chắn dịch được SQL, thay vì subquery lồng.
        var pageCustomerIds = items.Select(i => i.Id).ToList();
        if (pageCustomerIds.Count > 0)
        {
            // Chốt chống lộ: khách có thể gắn nhiều công ty, kể cả công ty người gọi KHÔNG được phủ.
            // Chỉ giữ context trong phạm vi (scope.Allows đúng cả khi toàn cục lẫn theo công ty).
            var contexts = (await context.CustomerCompanyContexts.AsNoTracking()
                    .Where(ctx => pageCustomerIds.Contains(ctx.CustomerId))
                    .Select(ctx => new { ctx.CustomerId, ctx.CompanyId, ctx.AssignedStaffId })
                    .ToListAsync(ct))
                .Where(x => scope.Allows(x.CompanyId))
                .ToList();

            var companyIds = contexts.Select(x => x.CompanyId).Distinct().ToList();
            var companyNames = companyIds.Count == 0
                ? new System.Collections.Generic.Dictionary<long, string>()
                : await context.Companies.AsNoTracking()
                    .Where(co => companyIds.Contains(co.Id))
                    .ToDictionaryAsync(co => co.Id, co => co.Name, ct);

            var staffIds = contexts.Where(x => x.AssignedStaffId.HasValue)
                .Select(x => x.AssignedStaffId!.Value).Distinct().ToList();
            var staffNames = staffIds.Count == 0
                ? new System.Collections.Generic.Dictionary<long, string>()
                : await context.Users.AsNoTracking()
                    .Where(u => staffIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

            var byCustomer = contexts
                .GroupBy(x => x.CustomerId)
                .ToDictionary(g => g.Key, g => g
                    .OrderBy(x => x.CompanyId)
                    .Select(x => new CustomerCompanyBriefDto
                    {
                        CompanyId = x.CompanyId,
                        CompanyName = companyNames.TryGetValue(x.CompanyId, out var cn) ? cn : null,
                        AssignedStaffId = x.AssignedStaffId,
                        AssignedStaffName = x.AssignedStaffId.HasValue
                            && staffNames.TryGetValue(x.AssignedStaffId.Value, out var sn) ? sn : null,
                    }).ToArray());

            foreach (var item in items)
                item.Companies = byCustomer.TryGetValue(item.Id, out var list)
                    ? list : Array.Empty<CustomerCompanyBriefDto>();

            // Phần mộ khách đang sở hữu — ĐÃ lọc theo phạm vi GRAVE_VIEW (ApplyScope trên truy vấn mộ).
            var owned = await GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), graveScope)
                .Where(g => g.OwnerCustomerId != null && pageCustomerIds.Contains(g.OwnerCustomerId.Value))
                .Select(g => new { g.Id, g.GraveCode, OwnerId = g.OwnerCustomerId!.Value })
                .ToListAsync(ct);
            var gravesByOwner = owned
                .GroupBy(x => x.OwnerId)
                .ToDictionary(g => g.Key, g => g
                    .OrderBy(x => x.GraveCode)
                    .Select(x => new OwnedGraveDto { GraveId = x.Id, GraveCode = x.GraveCode }).ToArray());

            foreach (var item in items)
                item.OwnedGraves = gravesByOwner.TryGetValue(item.Id, out var og)
                    ? og : Array.Empty<OwnedGraveDto>();
        }

        return new PagedResult<CustomerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<CompanyLookupDto[]> GetAssignedCompanyLookupsAsync(long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Chỉ liệt kê công ty người gọi được phủ — trước đây trả MỌI công ty, thành bản đồ chỉ
        // đường cho việc quét chéo.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewBasicPermission, ct);

        var companyIds = (await context.CustomerCompanyContexts
                .AsNoTracking()
                .Select(c => c.CompanyId)
                .Distinct()
                .ToListAsync(ct))
            .Where(scope.Allows)
            .ToList();
        if (companyIds.Count == 0) return Array.Empty<CompanyLookupDto>();

        return await context.Companies
            .AsNoTracking()
            .Where(co => companyIds.Contains(co.Id))
            .OrderBy(co => co.Name)
            .Select(co => new CompanyLookupDto { Id = co.Id, Name = co.Name })
            .ToArrayAsync(ct);
    }

    public async Task<StaffLookupDto[]> GetAssignedStaffLookupsAsync(long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Chỉ nhân sự được gán trong các công ty người gọi được phủ.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewBasicPermission, ct);

        var staffIds = (await context.CustomerCompanyContexts
                .AsNoTracking()
                .Where(c => c.AssignedStaffId != null)
                .Select(c => new { c.AssignedStaffId, c.CompanyId })
                .Distinct()
                .ToListAsync(ct))
            .Where(x => scope.Allows(x.CompanyId))
            .Select(x => x.AssignedStaffId!.Value)
            .Distinct()
            .ToList();
        if (staffIds.Count == 0) return Array.Empty<StaffLookupDto>();

        return await context.Users
            .AsNoTracking()
            .Where(u => staffIds.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new StaffLookupDto { Id = u.Id, FullName = u.FullName })
            .ToArrayAsync(ct);
    }

    public async Task<DuplicateCheckResult> CheckDuplicatesAsync(DuplicateCheckRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Chỉ báo trùng trong phạm vi công ty người gọi thấy được (kết quả có PII: tên/mã/SĐT/CCCD).
        // Đánh đổi: người quyền-công-ty sẽ không được cảnh báo trùng với khách ở công ty khác.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewBasicPermission, ct);
        var query = CustomerCompanyScope.ApplyScope(
                context.Customers.AsNoTracking().Include(c => c.Profile), context, scope)
            .Where(c => c.Profile.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Cccd))
            query = query.Where(c => c.Profile.Cccd == request.Cccd);
        else if (!string.IsNullOrWhiteSpace(request.Phone))
            query = query.Where(c => c.Profile.Phone == request.Phone);
        else
            return new DuplicateCheckResult();

        var matches = await query
            .Select(c => new CustomerListItemDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                FullName = c.Profile.FullName,
                Cccd = MaskCccd(c.Profile.Cccd),
                Phone = MaskPhone(c.Profile.Phone),
                CustomerStatus = c.CustomerStatus,
                CreatedAt = c.CreatedAt
            })
            .ToArrayAsync(ct);

        return new DuplicateCheckResult { HasDuplicates = matches.Length > 0, Matches = matches };
    }

    public async Task<CustomerCompanyContextDto[]> GetCompanyContextsAsync(long customerId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Không thấy khách -> không thấy các công ty của khách đó.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewBasicPermission, ct);
        if (!await CustomerCompanyScope.CanAccessCustomerAsync(context, customerId, scope, ct))
            return System.Array.Empty<CustomerCompanyContextDto>();

        var contexts = await context.CustomerCompanyContexts
            .AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.CompanyId)
            .ToListAsync(ct);
        if (contexts.Count == 0) return System.Array.Empty<CustomerCompanyContextDto>();

        var companyIds = contexts.Select(c => c.CompanyId).Distinct().ToList();
        var companyNames = await context.Companies.AsNoTracking()
            .Where(co => companyIds.Contains(co.Id))
            .Select(co => new { co.Id, co.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        var staffIds = contexts.Where(c => c.AssignedStaffId.HasValue)
            .Select(c => c.AssignedStaffId!.Value).Distinct().ToList();
        var staffNames = staffIds.Count == 0
            ? new Dictionary<long, string>()
            : await context.Users.AsNoTracking()
                .Where(u => staffIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName })
                .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);

        return contexts.Select(c =>
        {
            var dto = MapToContextDto(c);
            dto.CompanyName = companyNames.TryGetValue(c.CompanyId, out var cn) ? cn : null;
            dto.AssignedStaffName = c.AssignedStaffId.HasValue && staffNames.TryGetValue(c.AssignedStaffId.Value, out var sn) ? sn : null;
            return dto;
        }).ToArray();
    }

    public async Task<CustomerCompanyContextDto> CreateCompanyContextAsync(long customerId, CreateCustomerCompanyContextRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (!await context.Customers.AnyAsync(c => c.Id == customerId, ct))
                throw new EntityNotFoundException("CUS_CUSTOMER_NOT_FOUND", "Customer not found.");

            if (!await context.Companies.AnyAsync(c => c.Id == request.CompanyId && c.IsActive, ct))
                throw new EntityNotFoundException("CUS_COMPANY_NOT_FOUND", "Company not found or inactive.");

            // Gắn khách vào một công ty MỚI: phải có quyền trên khách hiện tại VÀ trên công ty đích
            // (nếu không, người quyền-công-ty-A có thể kéo khách sang công ty B mình không thuộc).
            var addScope = await _permissionEvaluator.ResolveAsync(actorUserId, CreateFinalPermission, ct);
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, customerId, addScope, "CUS_CONTEXT_FORBIDDEN_COMPANY", ct);
            if (!addScope.Allows(request.CompanyId))
                throw new PermissionDeniedException("CUS_CONTEXT_TARGET_FORBIDDEN",
                    "Bạn không có quyền gắn khách hàng vào công ty này.");

            if (await context.CustomerCompanyContexts.AnyAsync(c => c.CustomerId == customerId && c.CompanyId == request.CompanyId, ct))
                throw new BusinessRuleValidationException("CUS_DUPLICATE_COMPANY_CONTEXT", "Customer already has a context for this company.");

            var companyContext = new CustomerCompanyContext(
                customerId, request.CompanyId, request.AssignedStaffId,
                request.InternalNotes, request.FirstInteractionAt);
            companyContext.SetCreatedBy(actorUserId);
            context.CustomerCompanyContexts.Add(companyContext);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CONTEXT_CREATE",
                EntityType = "CustomerCompanyContext",
                EntityId = companyContext.Id.ToString(),
                CompanyId = request.CompanyId,
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { customerId, request.CompanyId, request.AssignedStaffId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToContextDto(companyContext);
        });
    }

    public async Task<CustomerCompanyContextDto> UpdateCompanyContextAsync(long customerId, long contextId, UpdateCustomerCompanyContextRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var companyContext = await context.CustomerCompanyContexts
                .FirstOrDefaultAsync(c => c.Id == contextId && c.CustomerId == customerId, ct);

            if (companyContext == null)
                throw new EntityNotFoundException("CUS_CONTEXT_NOT_FOUND", "Company context not found.");

            var ctxScope = await _permissionEvaluator.ResolveAsync(actorUserId, MasterUpdatePermission, ct);
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, customerId, ctxScope, "CUS_CONTEXT_FORBIDDEN_COMPANY", ct);
            if (!ctxScope.Allows(companyContext.CompanyId))
                throw new PermissionDeniedException("CUS_CONTEXT_TARGET_FORBIDDEN",
                    "Bạn không có quyền sửa liên kết công ty này.");

            if (!companyContext.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The company context has been modified by another process.");

            companyContext.Update(request.AssignedStaffId, request.RelationshipStatus,
                request.InternalNotes, request.LastInteractionAt, actorUserId);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("CUS_INVALID_ROW_VERSION", "The company context has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CONTEXT_UPDATE",
                EntityType = "CustomerCompanyContext",
                EntityId = companyContext.Id.ToString(),
                CompanyId = companyContext.CompanyId,
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { request.RelationshipStatus, request.AssignedStaffId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToContextDto(companyContext);
        });
    }

    private static CustomerDetailDto MapToDetailDto(Customer customer, Profile profile, bool maskSensitive)
    {
        return new CustomerDetailDto
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            CustomerStatus = customer.CustomerStatus,
            RowVersion = Convert.ToBase64String(customer.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            Profile = new ProfileDto
            {
                Id = profile.Id,
                FullName = profile.FullName,
                Cccd = maskSensitive ? MaskCccd(profile.Cccd) : profile.Cccd,
                Dob = profile.Dob,
                DobPartial = profile.DobPartial,
                DobPrecision = profile.DobPrecision,
                Gender = profile.Gender,
                PermanentAddress = maskSensitive ? MaskAddress(profile.PermanentAddress) : profile.PermanentAddress,
                CccdIssueDate = profile.CccdIssueDate,
                CccdIssuePlace = profile.CccdIssuePlace,
                TaxCode = profile.TaxCode,
                Phone = maskSensitive ? MaskPhone(profile.Phone) : profile.Phone,
                ContactAddress = maskSensitive ? MaskAddress(profile.ContactAddress) : profile.ContactAddress,
                DeathDateSolar = profile.DeathDateSolar,
                DeathDateLunar = profile.DeathDateLunar,
                DeathPlace = profile.DeathPlace,
                Hometown = profile.Hometown,
                IsActive = profile.IsActive,
                RowVersion = Convert.ToBase64String(profile.RowVersion ?? Array.Empty<byte>())
            }
        };
    }

    private static CustomerCompanyContextDto MapToContextDto(CustomerCompanyContext ctx)
    {
        return new CustomerCompanyContextDto
        {
            Id = ctx.Id,
            CustomerId = ctx.CustomerId,
            CompanyId = ctx.CompanyId,
            AssignedStaffId = ctx.AssignedStaffId,
            RelationshipStatus = ctx.RelationshipStatus,
            InternalNotes = ctx.InternalNotes,
            FirstInteractionAt = ctx.FirstInteractionAt,
            LastInteractionAt = ctx.LastInteractionAt,
            RowVersion = Convert.ToBase64String(ctx.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = ctx.CreatedAt,
            UpdatedAt = ctx.UpdatedAt
        };
    }

    private static string? MaskCccd(string? cccd)
    {
        if (string.IsNullOrEmpty(cccd) || cccd.Length <= 4) return cccd == null ? null : "****";
        return new string('*', cccd.Length - 4) + cccd[^4..];
    }

    private static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length <= 3) return phone == null ? null : "***";
        return new string('*', phone.Length - 3) + phone[^3..];
    }

    private static string? MaskAddress(string? address)
    {
        if (string.IsNullOrEmpty(address)) return address;
        return "***";
    }

    // Tạo pattern LIKE 'term%' an toàn: escape %, _, [ bằng cú pháp [] (không cần ESCAPE clause).
    private static string LikePrefix(string term)
        => term.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]") + "%";
}
