using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Graves.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Graves.Services;

public class GraveService : IGraveService
{
    private static readonly HashSet<string> AllowedZones =
        new(new[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L" });
    private static readonly HashSet<string> AllowedTypes =
        new(new[] { "SINGLE", "DOUBLE", "FAMILY", "CREMATION", "OTHER" });
    private static readonly HashSet<string> AllowedStatuses =
        new(new[] { "EMPTY", "RESERVED", "OCCUPIED", "RELOCATED" });

    private static readonly HashSet<string> AllowedTransferTypes =
        new(new[] { "SALE", "GIFT", "RELOCATION", "INHERITANCE", "DEATH", "CORRECTION" });

    // Mã quyền dạng chuỗi (PermissionCodes nằm ở tầng PTKD.Api, không tham chiếu ngược được).
    private const string ViewPermission = "GRAVE_VIEW";
    private const string CreatePermission = "GRAVE_CREATE";
    private const string UpdatePermission = "GRAVE_UPDATE";
    private const string TransferPermission = "GRAVE_TRANSFER_OWNERSHIP";
    private const string CustomerDeceasedStatus = "DECEASED";

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;
    private readonly PTKD.Application.Relationships.Services.IRelationshipDerivationService _derivationService;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public GraveService(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter,
        PTKD.Application.Relationships.Services.IRelationshipDerivationService derivationService,
        IPermissionEvaluator permissionEvaluator)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
        _derivationService = derivationService;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<PagedResult<GraveListItemDto>> SearchGravesAsync(GraveSearchRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Lọc theo công ty NGAY trong truy vấn (dữ liệu tới 50k+ mộ, không nạp hết về rồi lọc):
        // mộ -> nghĩa trang -> công ty, chỉ giữ công ty người gọi được cấp GRAVE_VIEW.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        var query = GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), scope);

        if (!string.IsNullOrWhiteSpace(request.Zone))
            query = query.Where(g => g.Zone == request.Zone);
        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(g => g.Status == request.Status);
        if (!string.IsNullOrWhiteSpace(request.GraveType))
            query = query.Where(g => g.GraveType == request.GraveType);
        if (request.OwnerCustomerId.HasValue)
            query = query.Where(g => g.OwnerCustomerId == request.OwnerCustomerId.Value);
        if (request.CompanyId.HasValue)
            query = query.Where(g => g.Cemetery!.CompanyId == request.CompanyId.Value);
        // Lọc theo tương quan giữa SỐ NGƯỜI AN TÁNG (cốt ACTIVE) và SỐ CỐT của mộ.
        if (!string.IsNullOrWhiteSpace(request.Capacity))
        {
            switch (request.Capacity)
            {
                case "UNDER":
                    query = query.Where(g => g.Occupants.Count(o => o.Status == GraveOccupant.StatusActive) < g.CotCount);
                    break;
                case "FULL":
                    query = query.Where(g => g.Occupants.Count(o => o.Status == GraveOccupant.StatusActive) == g.CotCount);
                    break;
                case "OVER":
                    query = query.Where(g => g.Occupants.Count(o => o.Status == GraveOccupant.StatusActive) > g.CotCount);
                    break;
            }
        }
        if (request.TagIds != null && request.TagIds.Length > 0)
        {
            var tagIds = request.TagIds;
            query = query.Where(g => context.GraveTags.Any(x => x.GraveId == g.Id && tagIds.Contains(x.TagId)));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search!;
            query = query.Where(g =>
                g.GraveCode.Contains(search) ||
                g.PlotNumber.Contains(search) ||
                (g.Owner != null && g.Owner.Profile.FullName.Contains(search)) ||
                g.Occupants.Any(o => o.FullName.Contains(search)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(g => g.Zone)
            .ThenBy(g => g.PlotNumber)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new GraveListItemDto
            {
                Id = g.Id,
                GraveCode = g.GraveCode,
                Zone = g.Zone,
                PlotNumber = g.PlotNumber,
                GraveType = g.GraveType,
                AreaM2 = g.AreaM2,
                CotCount = g.CotCount,
                Status = g.Status,
                OwnerCustomerId = g.OwnerCustomerId,
                OwnerName = g.Owner != null ? g.Owner.Profile.FullName : null,
                // "Người an táng" = số người ĐANG được gán vào mộ (suất ACTIVE), KHÔNG tính suất đã bốc/cải táng.
                OccupantCount = g.Occupants.Count(o => o.Status == GraveOccupant.StatusActive),
                CompanyId = g.Cemetery != null ? g.Cemetery.CompanyId : (long?)null,
                CompanyName = g.Cemetery != null && g.Cemetery.Company != null ? g.Cemetery.Company.Name : null,
                CreatedAt = g.CreatedAt,
                Tags = context.GraveTags
                    .Where(x => x.GraveId == g.Id)
                    .OrderBy(x => x.Tag!.Name)
                    .Select(x => new PTKD.Application.Tags.DTOs.TagDto
                    {
                        Id = x.Tag!.Id, TagType = x.Tag.TagType, Name = x.Tag.Name,
                        Color = x.Tag.Color, IsActive = x.Tag.IsActive
                    }).ToArray()
            })
            .ToArrayAsync(ct);

        return new PagedResult<GraveListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<GraveCompanyLookupDto[]> GetCompanyLookupsAsync(long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Chỉ liệt kê công ty người gọi được phủ (qua nghĩa trang chứa mộ) — dùng CÙNG scope với danh sách mộ.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        var scopedGraves = GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), scope);

        var companyIds = await scopedGraves
            .Select(g => g.Cemetery!.CompanyId)
            .Distinct()
            .ToListAsync(ct);
        if (companyIds.Count == 0) return Array.Empty<GraveCompanyLookupDto>();

        return await context.Companies
            .AsNoTracking()
            .Where(co => companyIds.Contains(co.Id))
            .OrderBy(co => co.Name)
            .Select(co => new GraveCompanyLookupDto { Id = co.Id, Name = co.Name })
            .ToArrayAsync(ct);
    }

    public async Task<string[]> GetZoneLookupsAsync(long companyId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Cùng scope với danh sách mộ; chỉ khu thuộc CÔNG TY đã chọn (không lộ khu công ty khác).
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!GraveCompanyScope.AllowsCompany(scope, companyId))
            return Array.Empty<string>();

        var scopedGraves = GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), scope);

        var zones = await scopedGraves
            .Where(g => g.Cemetery!.CompanyId == companyId)
            .Select(g => g.Zone)
            .Distinct()
            .OrderBy(z => z)
            .ToArrayAsync(ct);

        return zones;
    }

    public async Task<PagedResult<GraveAttachmentSummaryDto>> GetAttachmentSummaryAsync(GraveAttachmentSummaryRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);

        var query = GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), scope);

        // Lọc cấp MỘ: tìm theo mã mộ hoặc tên chủ mộ; theo khu.
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(g => g.GraveCode.Contains(s)
                || (g.Owner != null && g.Owner.Profile.FullName.Contains(s)));
        }
        if (!string.IsNullOrWhiteSpace(request.Zone))
        {
            var zone = request.Zone.Trim();
            query = query.Where(g => g.Zone == zone);
        }

        // Lọc cấp ĐÍNH KÈM (loại / người tải / khoảng ngày) — mộ phải có ÍT NHẤT một file khớp
        // TẤT CẢ điều kiện. Không có bộ lọc nào → chỉ cần "có đính kèm".
        var catExact = (request.Category is "PHOTO" or "TRANSFER_DOC") ? request.Category : null;
        var catOther = request.Category == "OTHER";
        var uploader = request.UploadedByUserId;
        var fromUtc = request.UploadedFrom;
        var toUtc = request.UploadedTo;

        query = query.Where(g => context.GraveAttachments.Any(a => a.GraveId == g.Id
            && (catExact == null || a.Category == catExact)
            && (!catOther || (a.Category != "PHOTO" && a.Category != "TRANSFER_DOC"))
            && (uploader == null || a.CreatedByUserId == uploader)
            && (fromUtc == null || a.CreatedAt >= fromUtc)
            && (toUtc == null || a.CreatedAt <= toUtc)));

        var total = await query.CountAsync(ct);

        var pageGraves = await query
            .OrderBy(g => g.GraveCode)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new
            {
                g.Id, g.GraveCode, g.Zone, g.GraveType,
                OwnerName = g.Owner != null ? g.Owner.Profile.FullName : null,
                CemeteryName = g.Cemetery != null ? g.Cemetery.Name : null
            })
            .ToListAsync(ct);

        var ids = pageGraves.Select(x => x.Id).ToList();
        var counts = ids.Count == 0
            ? new List<GraveAttachmentCount>()
            : await context.GraveAttachments.AsNoTracking()
                .Where(a => ids.Contains(a.GraveId))
                .GroupBy(a => a.GraveId)
                .Select(grp => new GraveAttachmentCount
                {
                    GraveId = grp.Key,
                    Photo = grp.Count(a => a.Category == GraveAttachment.CategoryPhoto),
                    Transfer = grp.Count(a => a.Category == GraveAttachment.CategoryTransferDoc),
                    Other = grp.Count(a => a.Category != GraveAttachment.CategoryPhoto && a.Category != GraveAttachment.CategoryTransferDoc),
                    Total = grp.Count(),
                    Last = grp.Max(a => (DateTime?)a.CreatedAt)
                })
                .ToListAsync(ct);
        var byId = counts.ToDictionary(c => c.GraveId);

        var items = pageGraves.Select(g =>
        {
            byId.TryGetValue(g.Id, out var c);
            return new GraveAttachmentSummaryDto
            {
                GraveId = g.Id, GraveCode = g.GraveCode, Zone = g.Zone, GraveType = g.GraveType,
                OwnerName = g.OwnerName, CemeteryName = g.CemeteryName,
                PhotoCount = c?.Photo ?? 0, TransferDocCount = c?.Transfer ?? 0, OtherCount = c?.Other ?? 0,
                TotalCount = c?.Total ?? 0, LastUploadedAt = c?.Last
            };
        }).ToArray();

        return new PagedResult<GraveAttachmentSummaryDto>
        {
            Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize
        };
    }

    public async Task<System.Collections.Generic.IReadOnlyList<AttachmentUploaderDto>> GetAttachmentUploadersAsync(long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);

        var scopedGraveIds = GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), scope).Select(g => g.Id);
        var userIds = await context.GraveAttachments.AsNoTracking()
            .Where(a => a.CreatedByUserId != null && scopedGraveIds.Contains(a.GraveId))
            .Select(a => a.CreatedByUserId!.Value)
            .Distinct()
            .ToListAsync(ct);
        if (userIds.Count == 0)
            return new List<AttachmentUploaderDto>();

        return await context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new AttachmentUploaderDto(
                u.Id, u.EmployeeCode != null ? u.FullName + " (" + u.EmployeeCode + ")" : u.FullName))
            .ToListAsync(ct);
    }

    private sealed class GraveAttachmentCount
    {
        public long GraveId { get; set; }
        public int Photo { get; set; }
        public int Transfer { get; set; }
        public int Other { get; set; }
        public int Total { get; set; }
        public DateTime? Last { get; set; }
    }

    public async Task<GraveDetailDto?> GetGraveByIdAsync(long id, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Mộ thuộc công ty người gọi không có quyền -> coi như không thấy (404), không lộ tồn tại.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!await GraveCompanyScope.CanAccessGraveAsync(context, id, scope, ct))
            return null;

        return await LoadGraveDetailAsync(id, ct);
    }

    /// <summary>Nạp chi tiết mộ KHÔNG kiểm quyền — chỉ gọi sau khi nơi gọi đã xác thực truy cập.</summary>
    private async Task<GraveDetailDto?> LoadGraveDetailAsync(long id, CancellationToken ct)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var grave = await context.Graves
            .AsNoTracking()
            .Include(g => g.Owner).ThenInclude(c => c!.Profile)
            .Include(g => g.Occupants)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

        if (grave == null) return null;

        var dto = MapToDetailDto(grave);
        dto.EmergencyContacts = await LoadEmergencyContactsAsync(context, id, ct);
        dto.Tags = await context.GraveTags.AsNoTracking()
            .Where(x => x.GraveId == id)
            .OrderBy(x => x.Tag!.Name)
            .Select(x => new PTKD.Application.Tags.DTOs.TagDto
            {
                Id = x.Tag!.Id, TagType = x.Tag.TagType, Name = x.Tag.Name,
                Color = x.Tag.Color, IsActive = x.Tag.IsActive
            })
            .ToArrayAsync(ct);
        return dto;
    }

    private static async Task<GraveEmergencyContactDto[]> LoadEmergencyContactsAsync(
        IOrganizationDbContext context, long graveId, CancellationToken ct)
    {
        return await context.GraveEmergencyContacts.AsNoTracking()
            .Where(c => c.GraveId == graveId && c.IsActive)
            .OrderBy(c => c.Priority).ThenBy(c => c.Id)
            .Select(c => new GraveEmergencyContactDto
            {
                Id = c.Id,
                GraveId = c.GraveId,
                Priority = c.Priority,
                ContactCustomerId = c.ContactCustomerId,
                ContactCode = c.Contact != null ? c.Contact.CustomerCode : null,
                // Ưu tiên tên/SĐT theo hồ sơ KH (động); rơi về giá trị nhập tay nếu không liên kết KH.
                ContactName = c.Contact != null ? c.Contact.Profile.FullName : (c.ContactName ?? ""),
                ContactPhone = c.Contact != null ? c.Contact.Profile.Phone : c.ContactPhone,
                RelationshipNote = c.RelationshipNote,
                RowVersion = Convert.ToBase64String(c.RowVersion)
            })
            .ToArrayAsync(ct);
    }

    public async Task<GraveDetailDto> CreateGraveAsync(CreateGraveRequest request, long actorUserId, CancellationToken ct = default)
    {
        ValidateEnums(request.Zone, request.GraveType, request.Status);

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (await context.Graves.AnyAsync(g => g.GraveCode == request.GraveCode, ct))
                throw new BusinessRuleValidationException("GRAVE_DUPLICATE_CODE", "Grave code already exists.");

            var cemeteryId = await ResolveCemeteryIdAsync(context, request.CemeteryId, ct);

            // Mộ thuộc công ty QUA nghĩa trang: chỉ cho tạo nếu quyền GRAVE_CREATE của người gọi
            // phủ tới công ty của nghĩa trang này.
            var createScope = await _permissionEvaluator.ResolveAsync(actorUserId, CreatePermission, ct);
            var cemeteryCompanyId = await context.Cemeteries
                .Where(c => c.Id == cemeteryId).Select(c => c.CompanyId).FirstAsync(ct);
            if (!GraveCompanyScope.AllowsCompany(createScope, cemeteryCompanyId))
                throw new PermissionDeniedException("GRAVE_CREATE_FORBIDDEN_COMPANY",
                    "Bạn không có quyền tạo mộ ở công ty của nghĩa trang này.");

            // Chủ mộ (nếu có) phải là khách của công ty quản lý mộ này.
            await EnsureOwnerInGraveCompanyAsync(context, request.OwnerCustomerId, cemeteryCompanyId, ct);

            var grave = new Grave(
                cemeteryId,
                request.GraveCode, request.Zone, request.PlotNumber, request.GraveType, request.Status,
                request.RowLabel, request.ColLabel, request.AreaM2, request.CotCount, request.OwnerCustomerId,
                request.EmergencyContactName, request.EmergencyContactPhone, request.EmergencyContactRelationship,
                request.Notes);
            grave.SetCreatedBy(actorUserId);
            context.Graves.Add(grave);
            await context.SaveChangesAsync(ct);

            foreach (var o in request.Occupants)
            {
                var occupant = new GraveOccupant(
                    grave.Id, o.FullName, o.Gender, o.Dob,
                    o.DeathDateSolar, o.DeathDateLunar, o.BurialDate, o.Hometown,
                    o.OwnerRelationship, o.DeceasedRelationship, o.Notes);
                occupant.SetCreatedBy(actorUserId);
                context.GraveOccupants.Add(occupant);
            }
            if (request.Occupants.Length > 0)
                await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_CREATE",
                EntityType = "Grave",
                EntityId = grave.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { grave.GraveCode, grave.Zone, grave.PlotNumber, grave.Status, grave.OwnerCustomerId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return (await LoadGraveDetailAsync(grave.Id, ct))!;
        });
    }

    public async Task<GraveDetailDto> UpdateGraveAsync(long id, UpdateGraveRequest request, long actorUserId, CancellationToken ct = default)
    {
        ValidateEnums(request.Zone, request.GraveType, request.Status);
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var grave = await context.Graves.FirstOrDefaultAsync(g => g.Id == id, ct);
            if (grave == null)
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");

            var updateScope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, id, updateScope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            if (!grave.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The grave has been modified by another process.");

            // Chỉ kiểm khi ĐỔI chủ (tránh chặn sửa các mộ cũ có chủ lệch công ty từ dữ liệu cũ).
            if (request.OwnerCustomerId != grave.OwnerCustomerId)
            {
                var graveCompanyId = await GraveCompanyIdAsync(context, grave.CemeteryId, ct);
                await EnsureOwnerInGraveCompanyAsync(context, request.OwnerCustomerId, graveCompanyId, ct);
            }

            var beforeState = JsonSerializer.Serialize(new { grave.Zone, grave.PlotNumber, grave.Status, grave.OwnerCustomerId });

            grave.Update(
                request.Zone, request.PlotNumber, request.GraveType, request.Status,
                request.RowLabel, request.ColLabel, request.AreaM2, request.CotCount, request.OwnerCustomerId,
                request.EmergencyContactName, request.EmergencyContactPhone, request.EmergencyContactRelationship,
                request.Notes, actorUserId);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The grave has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_UPDATE",
                EntityType = "Grave",
                EntityId = grave.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                BeforeStateJson = beforeState,
                AfterStateJson = JsonSerializer.Serialize(new { request.Zone, request.PlotNumber, request.Status, request.OwnerCustomerId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return (await LoadGraveDetailAsync(grave.Id, ct))!;
        });
    }

    public async Task<GraveOccupantDto> AddOccupantAsync(long graveId, PlaceGraveOccupantRequest request, long actorUserId, CancellationToken ct = default)
    {
        // 1) Đọc chủ mộ ngoài transaction (fail nhanh) — phải có chủ mới đặt cốt được.
        long ownerId;
        await using (var readCtx = _dbContextFactory.CreateDbContext())
        {
            var g = await readCtx.Graves.AsNoTracking()
                .Where(x => x.Id == graveId)
                .Select(x => new { x.OwnerCustomerId })
                .FirstOrDefaultAsync(ct);
            if (g == null)
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");
            if (g.OwnerCustomerId is not long oid)
                throw new BusinessRuleValidationException("GRAVE_NO_OWNER", "Mộ chưa có chủ — cần gán chủ mộ trước khi đặt cốt.");
            ownerId = oid;
        }

        // 2) Suy nhãn quan hệ chủ→khách ngoài transaction. Không có quan hệ gia đình thật (rơi về
        //    OTHER) thì KHÔNG cho đặt cốt (theo quyết định D1).
        var derived = await _derivationService.DeriveOwnerToOccupantsAsync(ownerId, new[] { request.DeceasedCustomerId }, ct);
        var rel = derived.FirstOrDefault();
        if (rel == null || rel.RelationKind == RelationshipKind.Other)
            throw new BusinessRuleValidationException("GRAVE_OCC_NO_FAMILY",
                "Người này chưa có quan hệ gia đình với chủ mộ — hãy khai quan hệ trước khi đặt cốt.");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var grave = await context.Graves.FirstOrDefaultAsync(g => g.Id == graveId, ct);
            if (grave == null)
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");

            var addOccScope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, addOccScope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            var customer = await context.Customers.Include(c => c.Profile)
                .FirstOrDefaultAsync(c => c.Id == request.DeceasedCustomerId, ct);
            if (customer == null)
                throw new EntityNotFoundException("GRAVE_OCC_CUSTOMER_NOT_FOUND", "Không tìm thấy khách hàng.");
            if (customer.CustomerStatus != CustomerDeceasedStatus)
                throw new BusinessRuleValidationException("GRAVE_OCC_NOT_DECEASED", "Chỉ đặt được người ĐÃ MẤT vào cốt.");

            // Chưa nằm mộ nào ĐANG HIỆU LỰC (suất đã bốc/cải táng không tính — cho phép đặt lại).
            if (await context.GraveOccupants.AnyAsync(o => o.DeceasedCustomerId == customer.Id && o.Status == GraveOccupant.StatusActive, ct))
                throw new BusinessRuleValidationException("GRAVE_OCC_ALREADY_PLACED", "Người này đang được an táng ở một mộ.");

            // Sức chứa theo số cốt — chỉ đếm suất đang hiệu lực.
            var currentCount = await context.GraveOccupants.CountAsync(o => o.GraveId == graveId && o.Status == GraveOccupant.StatusActive, ct);
            if (currentCount >= grave.CotCount)
                throw new BusinessRuleValidationException("GRAVE_FULL", $"Mộ đã đủ {grave.CotCount} cốt.");

            // Chụp thông tin từ hồ sơ khách + nhãn quan hệ đã suy.
            var p = customer.Profile;
            var occupant = new GraveOccupant(
                graveId, p.FullName, p.Gender, p.Dob, p.DeathDateSolar, p.DeathDateLunar,
                request.BurialDate, p.Hometown, rel.OwnerRelationshipLabel, rel.DeceasedRelationshipLabel, request.Notes);
            occupant.LinkDeceasedCustomer(customer.Id);
            occupant.SetCreatedBy(actorUserId);
            context.GraveOccupants.Add(occupant);

            // Có cốt đầu tiên → mộ chuyển sang ĐÃ AN TÁNG (nếu đang trống/đặt trước).
            if (grave.Status == Grave.StatusEmpty || grave.Status == Grave.StatusReserved)
                grave.Update(grave.Zone, grave.PlotNumber, grave.GraveType, Grave.StatusOccupied,
                    grave.RowLabel, grave.ColLabel, grave.AreaM2, grave.CotCount, grave.OwnerCustomerId,
                    grave.EmergencyContactName, grave.EmergencyContactPhone, grave.EmergencyContactRelationship,
                    grave.Notes, actorUserId);

            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_OCCUPANT_ADD",
                EntityType = "GraveOccupant",
                EntityId = occupant.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { graveId, deceasedCustomerId = customer.Id })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToOccupantDto(occupant);
        });
    }

    public async Task<IReadOnlyList<OccupantCandidateDto>> GetOccupantCandidatesAsync(long graveId, string? search, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Không thấy mộ (khác công ty) → không gợi ý ứng viên.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!await GraveCompanyScope.CanAccessGraveAsync(context, graveId, scope, ct))
            return Array.Empty<OccupantCandidateDto>();

        var ownerId = await context.Graves.AsNoTracking()
            .Where(g => g.Id == graveId).Select(g => g.OwnerCustomerId).FirstOrDefaultAsync(ct);
        if (ownerId is not long owner)
            return Array.Empty<OccupantCandidateDto>();

        // Ứng viên = khách có cạnh quan hệ TRỰC TIẾP với chủ mộ (chủ đã khai), ĐÃ MẤT, chưa nằm mộ nào.
        var relatedIds = await context.CustomerRelationships.AsNoTracking()
            .Where(r => r.FromCustomerId == owner)
            .Select(r => r.ToCustomerId)
            .Distinct()
            .ToListAsync(ct);
        if (relatedIds.Count == 0) return Array.Empty<OccupantCandidateDto>();

        var placedIds = (await context.GraveOccupants.AsNoTracking()
            .Where(o => o.DeceasedCustomerId != null && o.Status == GraveOccupant.StatusActive
                && relatedIds.Contains(o.DeceasedCustomerId.Value))
            .Select(o => o.DeceasedCustomerId!.Value)
            .ToListAsync(ct)).ToHashSet();

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var candidates = await context.Customers.AsNoTracking()
            .Where(c => relatedIds.Contains(c.Id) && c.CustomerStatus == CustomerDeceasedStatus
                && (term == null || c.CustomerCode.Contains(term) || c.Profile.FullName.Contains(term)))
            .Select(c => new { c.Id, c.CustomerCode, c.Profile.FullName })
            .ToListAsync(ct);

        var freeCandidates = candidates.Where(c => !placedIds.Contains(c.Id)).ToList();
        if (freeCandidates.Count == 0) return Array.Empty<OccupantCandidateDto>();

        // Nhãn quan hệ (cốt LÀ gì của chủ) cho từng ứng viên.
        var derived = (await _derivationService.DeriveOwnerToOccupantsAsync(owner, freeCandidates.Select(c => c.Id).ToList(), ct))
            .ToDictionary(d => d.OccupantCustomerId);

        return freeCandidates.Select(c => new OccupantCandidateDto
        {
            CustomerId = c.Id,
            CustomerCode = c.CustomerCode,
            FullName = c.FullName,
            RelationLabel = derived.TryGetValue(c.Id, out var d) ? d.DeceasedRelationshipLabel : "",
        }).ToList();
    }

    public async Task<GraveOccupantDto> UpdateOccupantAsync(long graveId, long occupantId, UpdateGraveOccupantRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var occupant = await context.GraveOccupants.FirstOrDefaultAsync(o => o.Id == occupantId && o.GraveId == graveId, ct);
            if (occupant == null)
                throw new EntityNotFoundException("GRAVE_OCCUPANT_NOT_FOUND", "Grave occupant not found.");

            var updOccScope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, updOccScope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            if (!occupant.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The occupant has been modified by another process.");

            occupant.Update(
                request.FullName, request.Gender, request.Dob,
                request.DeathDateSolar, request.DeathDateLunar, request.BurialDate,
                request.Hometown, request.OwnerRelationship, request.DeceasedRelationship, request.Notes, actorUserId);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The occupant has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_OCCUPANT_UPDATE",
                EntityType = "GraveOccupant",
                EntityId = occupant.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { graveId, occupant.FullName })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToOccupantDto(occupant);
        });
    }

    public async Task<GraveOccupantDto> RelocateOccupantAsync(long graveId, long occupantId, RelocateOccupantRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var occupant = await context.GraveOccupants.FirstOrDefaultAsync(o => o.Id == occupantId && o.GraveId == graveId, ct);
            if (occupant == null)
                throw new EntityNotFoundException("GRAVE_OCCUPANT_NOT_FOUND", "Grave occupant not found.");

            var scope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, scope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            if (occupant.Status != GraveOccupant.StatusActive)
                throw new BusinessRuleValidationException("GRAVE_OCC_NOT_ACTIVE", "Suất này đã được bốc/cải táng trước đó.");

            occupant.Relocate(request.RelocatedAt, request.Note, actorUserId);
            await context.SaveChangesAsync(ct);

            // Hết suất đang hiệu lực → mộ về TRỐNG (giải phóng để tái sử dụng); còn thì giữ ĐÃ AN TÁNG.
            var activeLeft = await context.GraveOccupants
                .CountAsync(o => o.GraveId == graveId && o.Status == GraveOccupant.StatusActive, ct);
            if (activeLeft == 0)
            {
                var grave = await context.Graves.FirstAsync(g => g.Id == graveId, ct);
                if (grave.Status == Grave.StatusOccupied)
                    grave.Update(grave.Zone, grave.PlotNumber, grave.GraveType, Grave.StatusEmpty,
                        grave.RowLabel, grave.ColLabel, grave.AreaM2, grave.CotCount, grave.OwnerCustomerId,
                        grave.EmergencyContactName, grave.EmergencyContactPhone, grave.EmergencyContactRelationship,
                        grave.Notes, actorUserId);
                await context.SaveChangesAsync(ct);
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_OCCUPANT_RELOCATE",
                EntityType = "GraveOccupant",
                EntityId = occupant.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { graveId, occupantId, occupant.DeceasedCustomerId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return MapToOccupantDto(occupant);
        });
    }

    // ─── Liên hệ khẩn cấp động (là khách hàng) ───────────────────────────────

    public async Task<GraveEmergencyContactDto> AddEmergencyContactAsync(long graveId, CreateGraveEmergencyContactRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (!await context.Graves.AnyAsync(g => g.Id == graveId, ct))
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");
            if (!await context.Customers.AnyAsync(c => c.Id == request.ContactCustomerId, ct))
                throw new EntityNotFoundException("GRAVE_EC_CUSTOMER_NOT_FOUND", "Contact customer not found.");

            var addEcScope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, addEcScope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            // Ưu tiên = kế tiếp cuối danh sách đang hoạt động (gọi lần lượt theo priority).
            var maxPriority = await context.GraveEmergencyContacts
                .Where(c => c.GraveId == graveId && c.IsActive)
                .Select(c => (int?)c.Priority)
                .MaxAsync(ct) ?? 0;

            var contact = new GraveEmergencyContact(
                graveId, maxPriority + 1, request.ContactCustomerId,
                null, null, request.RelationshipNote, actorUserId);
            context.GraveEmergencyContacts.Add(contact);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_EMERGENCY_CONTACT_ADD",
                EntityType = "GraveEmergencyContact",
                EntityId = contact.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { graveId, contact.ContactCustomerId, contact.Priority })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await GetEmergencyContactDtoAsync(context, contact.Id, ct);
        });
    }

    public async Task<GraveEmergencyContactDto> UpdateEmergencyContactAsync(long graveId, long contactId, UpdateGraveEmergencyContactRequest request, long actorUserId, CancellationToken ct = default)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var contact = await context.GraveEmergencyContacts
                .FirstOrDefaultAsync(c => c.Id == contactId && c.GraveId == graveId && c.IsActive, ct);
            if (contact == null)
                throw new EntityNotFoundException("GRAVE_EC_NOT_FOUND", "Emergency contact not found.");

            var updEcScope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, updEcScope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            if (!contact.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The emergency contact has been modified by another process.");
            if (!await context.Customers.AnyAsync(c => c.Id == request.ContactCustomerId, ct))
                throw new EntityNotFoundException("GRAVE_EC_CUSTOMER_NOT_FOUND", "Contact customer not found.");

            contact.Update(request.ContactCustomerId, null, null, request.RelationshipNote, actorUserId);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The emergency contact has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_EMERGENCY_CONTACT_UPDATE",
                EntityType = "GraveEmergencyContact",
                EntityId = contact.Id.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { graveId, contact.ContactCustomerId })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return await GetEmergencyContactDtoAsync(context, contact.Id, ct);
        });
    }

    public async Task RemoveEmergencyContactAsync(long graveId, long contactId, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var contact = await context.GraveEmergencyContacts
                .FirstOrDefaultAsync(c => c.Id == contactId && c.GraveId == graveId, ct);
            if (contact == null)
                throw new EntityNotFoundException("GRAVE_EC_NOT_FOUND", "Emergency contact not found.");

            var rmEcScope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, rmEcScope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            // Xóa cứng để giải phóng slot priority (chỉ số UQ theo grave_id+priority).
            context.GraveEmergencyContacts.Remove(contact);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_EMERGENCY_CONTACT_REMOVE",
                EntityType = "GraveEmergencyContact",
                EntityId = contactId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                BeforeStateJson = JsonSerializer.Serialize(new { graveId, contact.ContactCustomerId, contact.Priority })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);
        });
    }

    private static async Task<GraveEmergencyContactDto> GetEmergencyContactDtoAsync(IOrganizationDbContext context, long contactId, CancellationToken ct)
    {
        return await context.GraveEmergencyContacts.AsNoTracking()
            .Where(c => c.Id == contactId)
            .Select(c => new GraveEmergencyContactDto
            {
                Id = c.Id,
                GraveId = c.GraveId,
                Priority = c.Priority,
                ContactCustomerId = c.ContactCustomerId,
                ContactCode = c.Contact != null ? c.Contact.CustomerCode : null,
                ContactName = c.Contact != null ? c.Contact.Profile.FullName : (c.ContactName ?? ""),
                ContactPhone = c.Contact != null ? c.Contact.Profile.Phone : c.ContactPhone,
                RelationshipNote = c.RelationshipNote,
                RowVersion = Convert.ToBase64String(c.RowVersion)
            })
            .FirstAsync(ct);
    }

    public async Task<IReadOnlyList<AssignableGraveDto>> GetAssignableGravesAsync(long customerId, string? search, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!scope.Granted) return Array.Empty<AssignableGraveDto>();

        // Chỉ gợi ý mộ TRONG công ty của khách — để gán chủ không vi phạm P5 (chủ ∈ công ty của mộ).
        var customerCompanyIds = await context.CustomerCompanyContexts.AsNoTracking()
            .Where(cc => cc.CustomerId == customerId).Select(cc => cc.CompanyId).Distinct().ToListAsync(ct);
        if (customerCompanyIds.Count == 0) return Array.Empty<AssignableGraveDto>();

        var q = GraveCompanyScope.ApplyScope(context.Graves.AsNoTracking(), scope)
            .Where(g => g.OwnerCustomerId == null && g.Status == Grave.StatusEmpty
                && customerCompanyIds.Contains(g.Cemetery!.CompanyId));

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        if (term != null)
            q = q.Where(g => g.GraveCode.Contains(term) || g.Zone.Contains(term));

        var raw = await q.OrderBy(g => g.GraveCode).Take(50)
            .Select(g => new { g.Id, g.GraveCode, g.Zone, g.RowVersion })
            .ToListAsync(ct);

        return raw.Select(x => new AssignableGraveDto
        {
            GraveId = x.Id,
            GraveCode = x.GraveCode,
            Zone = x.Zone,
            RowVersion = Convert.ToBase64String(x.RowVersion),
        }).ToList();
    }

    public async Task<TransferOwnershipResultDto> TransferOwnershipAsync(long graveId, TransferOwnershipRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (!AllowedTransferTypes.Contains(request.TransferType))
            throw new BusinessRuleValidationException("GRAVE_INVALID_TRANSFER_TYPE", "Invalid transfer type.");
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        // 1) Nạp danh sách khách hàng của các cốt (ngoài transaction) để suy diễn theo chủ mới
        List<long> occCustomerIds;
        await using (var readCtx = _dbContextFactory.CreateDbContext())
        {
            if (!await readCtx.Graves.AsNoTracking().AnyAsync(g => g.Id == graveId, ct))
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");
            occCustomerIds = await readCtx.GraveOccupants.AsNoTracking()
                .Where(o => o.GraveId == graveId && o.DeceasedCustomerId != null)
                .Select(o => o.DeceasedCustomerId!.Value)
                .ToListAsync(ct);
        }

        // 2) Suy diễn nhãn quan hệ 2 chiều từ CHỦ MỚI đến từng cốt
        var derived = await _derivationService.DeriveOwnerToOccupantsAsync(request.NewOwnerCustomerId, occCustomerIds, ct);
        var derivedByCustomer = derived.ToDictionary(d => d.OccupantCustomerId);

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var grave = await context.Graves.FirstOrDefaultAsync(g => g.Id == graveId, ct);
            if (grave == null)
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");

            var transferScope = await _permissionEvaluator.ResolveAsync(actorUserId, TransferPermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, transferScope, "GRAVE_TRANSFER_FORBIDDEN_COMPANY", ct);

            if (!grave.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The grave has been modified by another process.");

            // Chủ mới phải là khách của công ty quản lý mộ (bao luôn ProcessOwnerDeath vì nó gọi hàm này).
            var graveCompanyId = await GraveCompanyIdAsync(context, grave.CemeteryId, ct);
            await EnsureOwnerInGraveCompanyAsync(context, request.NewOwnerCustomerId, graveCompanyId, ct);

            var previousOwnerId = grave.OwnerCustomerId;
            if (previousOwnerId == request.NewOwnerCustomerId)
                throw new BusinessRuleValidationException("GRAVE_SAME_OWNER", "New owner is the same as the current owner.");

            grave.Update(
                grave.Zone, grave.PlotNumber, grave.GraveType, grave.Status, grave.RowLabel, grave.ColLabel,
                grave.AreaM2, grave.CotCount, request.NewOwnerCustomerId,
                grave.EmergencyContactName, grave.EmergencyContactPhone, grave.EmergencyContactRelationship,
                grave.Notes, actorUserId);

            // Tái suy diễn nhãn quan hệ của các cốt theo chủ mới
            int rederived = 0, needConfirm = 0;
            var occupants = await context.GraveOccupants.Where(o => o.GraveId == graveId).ToListAsync(ct);
            foreach (var occ in occupants)
            {
                if (occ.DeceasedCustomerId is long decId && derivedByCustomer.TryGetValue(decId, out var d))
                {
                    occ.SetDerivedRelationship(d.OwnerRelationshipLabel, d.DeceasedRelationshipLabel, actorUserId);
                    rederived++;
                    if (d.NeedsConfirmation) needConfirm++;
                }
            }

            var history = new GraveOwnershipHistory(
                graveId, previousOwnerId, request.NewOwnerCustomerId, request.TransferType, request.Reason, actorUserId);
            context.GraveOwnershipHistories.Add(history);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("GRAVE_INVALID_ROW_VERSION", "The grave has been modified by another process.");
            }

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_TRANSFER_OWNERSHIP",
                EntityType = "Grave",
                EntityId = graveId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                BeforeStateJson = JsonSerializer.Serialize(new { previousOwnerId }),
                AfterStateJson = JsonSerializer.Serialize(new { newOwnerId = request.NewOwnerCustomerId, request.TransferType, rederived })
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            return new TransferOwnershipResultDto
            {
                Grave = (await LoadGraveDetailAsync(graveId, ct))!,
                OwnershipHistoryId = history.Id,
                OccupantsRederived = rederived,
                OccupantsNeedingConfirmation = needConfirm
            };
        });
    }

    public async Task<OwnerDeathResultDto> ProcessOwnerDeathAsync(OwnerDeathRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (request.DeceasedCustomerId == request.HeirCustomerId)
            throw new BusinessRuleValidationException("GRAVE_HEIR_SAME_AS_DECEASED", "Heir must differ from the deceased.");

        // 0) CHẶN TRƯỚC theo công ty: mọi mộ người mất sở hữu phải nằm trong phạm vi chuyển quyền
        //    của người gọi. Kiểm trước khi đánh dấu qua đời để không rơi vào trạng thái nửa vời
        //    (đã đánh dấu chết mà lại 403 giữa chừng khi chuyển tới một mộ ngoài phạm vi).
        var deathScope = await _permissionEvaluator.ResolveAsync(actorUserId, TransferPermission, ct);
        await using (var checkCtx = _dbContextFactory.CreateDbContext())
        {
            var graveCompanyIds = await checkCtx.Graves.AsNoTracking()
                .Where(g => g.OwnerCustomerId == request.DeceasedCustomerId)
                .Select(g => g.Cemetery!.CompanyId)
                .Distinct()
                .ToListAsync(ct);
            if (graveCompanyIds.Any(cid => !deathScope.Allows(cid)))
                throw new PermissionDeniedException("GRAVE_OWNER_DEATH_FORBIDDEN_COMPANY",
                    "Người mất sở hữu mộ ở công ty bạn không có quyền chuyển. Cần quyền chuyển quyền ở tất cả công ty liên quan.");
        }

        // 1) Đánh dấu khách hàng qua đời (transaction riêng)
        await using (var tempCtx = _dbContextFactory.CreateDbContext())
        {
            var strat = tempCtx.CreateExecutionStrategy();
            await strat.ExecuteAsync(async () =>
            {
                await using var context = _dbContextFactory.CreateDbContext();
                await using var tx = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

                var customer = await context.Customers.Include(c => c.Profile)
                    .FirstOrDefaultAsync(c => c.Id == request.DeceasedCustomerId, ct);
                if (customer == null)
                    throw new EntityNotFoundException("CUSTOMER_NOT_FOUND", "Customer not found.");
                if (customer.CustomerStatus == "DECEASED")
                    throw new BusinessRuleValidationException("CUSTOMER_ALREADY_DECEASED", "Customer is already marked as deceased.");
                if (!await context.Customers.AnyAsync(c => c.Id == request.HeirCustomerId, ct))
                    throw new EntityNotFoundException("GRAVE_HEIR_NOT_FOUND", "Heir customer not found.");

                customer.SetStatus("DECEASED", actorUserId);
                customer.Profile?.MarkDeceased(request.DeathDateSolar, actorUserId);
                await context.SaveChangesAsync(ct);

                var audit = new SecurityAuditEventRecord
                {
                    EventCode = "CUSTOMER_MARK_DECEASED",
                    EntityType = "Customer",
                    EntityId = customer.Id.ToString(),
                    Outcome = "SUCCESS",
                    CorrelationId = Guid.NewGuid(),
                    ActorUserId = actorUserId,
                    AfterStateJson = JsonSerializer.Serialize(new { customerId = customer.Id, status = "DECEASED" })
                };
                audit.ThrowIfContainsSensitiveData();
                await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);
                await tx.CommitAsync(ct);
            });
        }

        // 2) Tự chuyển mọi mộ deceased đang sở hữu sang người thừa kế (mỗi mộ tái suy diễn)
        List<(long graveId, byte[] rowVersion)> graves;
        await using (var readCtx = _dbContextFactory.CreateDbContext())
        {
            graves = (await readCtx.Graves.AsNoTracking()
                    .Where(g => g.OwnerCustomerId == request.DeceasedCustomerId)
                    .Select(g => new { g.Id, g.RowVersion })
                    .ToListAsync(ct))
                .Select(x => (x.Id, x.RowVersion))
                .ToList();
        }

        int transferred = 0, rederived = 0;
        foreach (var (graveId, rowVersion) in graves)
        {
            var result = await TransferOwnershipAsync(graveId, new TransferOwnershipRequest
            {
                NewOwnerCustomerId = request.HeirCustomerId,
                TransferType = GraveOwnershipHistory.TypeDeath,
                Reason = request.Reason ?? "Chủ mộ qua đời — chuyển quyền cho người thừa kế.",
                TargetVersion = Convert.ToBase64String(rowVersion)
            }, actorUserId, ct);
            transferred++;
            rederived += result.OccupantsRederived;
        }

        return new OwnerDeathResultDto
        {
            DeceasedCustomerId = request.DeceasedCustomerId,
            HeirCustomerId = request.HeirCustomerId,
            GravesOwned = graves.Count,
            GravesTransferred = transferred,
            OccupantsRederived = rederived
        };
    }

    public async Task<IReadOnlyList<OwnershipHistoryItemDto>> GetOwnershipHistoryAsync(long graveId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        // Không thấy mộ (khác công ty) -> không thấy lịch sử chuyển quyền của nó.
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        if (!await GraveCompanyScope.CanAccessGraveAsync(context, graveId, scope, ct))
            return Array.Empty<OwnershipHistoryItemDto>();

        var items = await context.GraveOwnershipHistories.AsNoTracking()
            .Where(h => h.GraveId == graveId)
            .OrderByDescending(h => h.TransferredAt)
            .Select(h => new OwnershipHistoryItemDto
            {
                Id = h.Id,
                PreviousOwnerId = h.PreviousOwnerId,
                NewOwnerId = h.NewOwnerId,
                TransferType = h.TransferType,
                Reason = h.Reason,
                TransferredAt = h.TransferredAt
            })
            .ToListAsync(ct);

        if (items.Count == 0) return items;

        var ids = items
            .SelectMany(i => new[] { i.PreviousOwnerId, i.NewOwnerId })
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var names = await context.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Profile.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);

        foreach (var i in items)
        {
            if (i.PreviousOwnerId.HasValue && names.TryGetValue(i.PreviousOwnerId.Value, out var pn)) i.PreviousOwnerName = pn;
            if (names.TryGetValue(i.NewOwnerId, out var nn)) i.NewOwnerName = nn;
        }
        return items;
    }

    // Chủ mộ phải là khách hàng THUỘC công ty quản lý mộ (qua nghĩa trang) — chống gán/chuyển quyền
    // cho khách của công ty khác chỉ bằng cách gửi id (bỏ qua khi không đặt chủ).
    private static async Task EnsureOwnerInGraveCompanyAsync(
        IOrganizationDbContext context, long? ownerCustomerId, long cemeteryCompanyId, CancellationToken ct)
    {
        if (!ownerCustomerId.HasValue) return;
        if (!await context.Customers.AnyAsync(c => c.Id == ownerCustomerId.Value, ct))
            throw new EntityNotFoundException("GRAVE_OWNER_NOT_FOUND", "Owner customer not found.");
        if (!await context.CustomerCompanyContexts.AnyAsync(
                cc => cc.CustomerId == ownerCustomerId.Value && cc.CompanyId == cemeteryCompanyId, ct))
            throw new PermissionDeniedException("GRAVE_OWNER_WRONG_COMPANY",
                "Chủ mộ phải là khách hàng thuộc công ty quản lý mộ này.");
    }

    private static async Task<long> GraveCompanyIdAsync(IOrganizationDbContext context, long cemeteryId, CancellationToken ct)
        => await context.Cemeteries.Where(cm => cm.Id == cemeteryId).Select(cm => cm.CompanyId).FirstAsync(ct);

    /// <summary>
    /// Mộ thuộc công ty QUA nghĩa trang, nên mỗi mộ phải có nghĩa trang. Nếu người tạo chỉ rõ thì
    /// kiểm nghĩa trang tồn tại + còn hoạt động; nếu bỏ trống thì hệ tự chọn khi có ĐÚNG một nghĩa
    /// trang, còn nhiều nghĩa trang thì bắt buộc chọn để không gán mộ nhầm công ty.
    /// </summary>
    private static async Task<long> ResolveCemeteryIdAsync(IOrganizationDbContext context, long? requestedCemeteryId, CancellationToken ct)
    {
        if (requestedCemeteryId.HasValue)
        {
            var valid = await context.Cemeteries
                .AnyAsync(c => c.Id == requestedCemeteryId.Value && c.IsActive, ct);
            if (!valid)
                throw new BusinessRuleValidationException("GRAVE_INVALID_CEMETERY", "Nghĩa trang không tồn tại hoặc đã ngừng hoạt động.");
            return requestedCemeteryId.Value;
        }

        var activeCemeteryIds = await context.Cemeteries
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .Take(2)
            .ToListAsync(ct);

        if (activeCemeteryIds.Count == 0)
            throw new BusinessRuleValidationException("GRAVE_NO_CEMETERY", "Chưa khai báo nghĩa trang nào. Vui lòng tạo nghĩa trang trước khi tạo mộ.");
        if (activeCemeteryIds.Count > 1)
            throw new BusinessRuleValidationException("GRAVE_CEMETERY_REQUIRED", "Có nhiều nghĩa trang; vui lòng chọn nghĩa trang cho mộ.");

        return activeCemeteryIds[0];
    }

    private static void ValidateEnums(string zone, string graveType, string status)
    {
        if (!AllowedZones.Contains(zone))
            throw new BusinessRuleValidationException("GRAVE_INVALID_ZONE", "Invalid zone. Allowed: A–L.");
        if (!AllowedTypes.Contains(graveType))
            throw new BusinessRuleValidationException("GRAVE_INVALID_TYPE", "Invalid grave type.");
        if (!AllowedStatuses.Contains(status))
            throw new BusinessRuleValidationException("GRAVE_INVALID_STATUS", "Invalid status.");
    }

    private static GraveDetailDto MapToDetailDto(Grave grave)
    {
        return new GraveDetailDto
        {
            Id = grave.Id,
            GraveCode = grave.GraveCode,
            Zone = grave.Zone,
            PlotNumber = grave.PlotNumber,
            RowLabel = grave.RowLabel,
            ColLabel = grave.ColLabel,
            GraveType = grave.GraveType,
            AreaM2 = grave.AreaM2,
            CotCount = grave.CotCount,
            Status = grave.Status,
            OwnerCustomerId = grave.OwnerCustomerId,
            OwnerName = grave.Owner?.Profile.FullName,
            OwnerCode = grave.Owner?.CustomerCode,
            EmergencyContactName = grave.EmergencyContactName,
            EmergencyContactPhone = grave.EmergencyContactPhone,
            EmergencyContactRelationship = grave.EmergencyContactRelationship,
            Notes = grave.Notes,
            RowVersion = Convert.ToBase64String(grave.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = grave.CreatedAt,
            UpdatedAt = grave.UpdatedAt,
            Occupants = grave.Occupants
                .OrderBy(o => o.Id)
                .Select(MapToOccupantDto)
                .ToArray()
        };
    }

    private static GraveOccupantDto MapToOccupantDto(GraveOccupant o)
    {
        return new GraveOccupantDto
        {
            Id = o.Id,
            GraveId = o.GraveId,
            DeceasedCustomerId = o.DeceasedCustomerId,
            Status = o.Status,
            RelocatedAt = o.RelocatedAt,
            RelocationNote = o.RelocationNote,
            FullName = o.FullName,
            Gender = o.Gender,
            Dob = o.Dob,
            DeathDateSolar = o.DeathDateSolar,
            DeathDateLunar = o.DeathDateLunar,
            BurialDate = o.BurialDate,
            Hometown = o.Hometown,
            OwnerRelationship = o.OwnerRelationship,
            DeceasedRelationship = o.DeceasedRelationship,
            Notes = o.Notes,
            RowVersion = Convert.ToBase64String(o.RowVersion ?? Array.Empty<byte>())
        };
    }
}
