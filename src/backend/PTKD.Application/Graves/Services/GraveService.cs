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
                OccupantCount = g.Occupants.Count,
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

            await EnsureOwnerExistsAsync(context, request.OwnerCustomerId, ct);

            var cemeteryId = await ResolveCemeteryIdAsync(context, request.CemeteryId, ct);

            // Mộ thuộc công ty QUA nghĩa trang: chỉ cho tạo nếu quyền GRAVE_CREATE của người gọi
            // phủ tới công ty của nghĩa trang này.
            var createScope = await _permissionEvaluator.ResolveAsync(actorUserId, CreatePermission, ct);
            var cemeteryCompanyId = await context.Cemeteries
                .Where(c => c.Id == cemeteryId).Select(c => c.CompanyId).FirstAsync(ct);
            if (!GraveCompanyScope.AllowsCompany(createScope, cemeteryCompanyId))
                throw new PermissionDeniedException("GRAVE_CREATE_FORBIDDEN_COMPANY",
                    "Bạn không có quyền tạo mộ ở công ty của nghĩa trang này.");

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

            await EnsureOwnerExistsAsync(context, request.OwnerCustomerId, ct);

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

    public async Task<GraveOccupantDto> AddOccupantAsync(long graveId, CreateGraveOccupantRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (!await context.Graves.AnyAsync(g => g.Id == graveId, ct))
                throw new EntityNotFoundException("GRAVE_NOT_FOUND", "Grave not found.");

            var addOccScope = await _permissionEvaluator.ResolveAsync(actorUserId, UpdatePermission, ct);
            await GraveCompanyScope.EnsureGraveAccessibleAsync(context, graveId, addOccScope, "GRAVE_UPDATE_FORBIDDEN_COMPANY", ct);

            var occupant = new GraveOccupant(
                graveId, request.FullName, request.Gender, request.Dob,
                request.DeathDateSolar, request.DeathDateLunar, request.BurialDate, request.Hometown,
                request.OwnerRelationship, request.DeceasedRelationship, request.Notes);
            occupant.SetCreatedBy(actorUserId);
            context.GraveOccupants.Add(occupant);
            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "GRAVE_OCCUPANT_ADD",
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

            if (!await context.Customers.AnyAsync(c => c.Id == request.NewOwnerCustomerId, ct))
                throw new EntityNotFoundException("GRAVE_OWNER_NOT_FOUND", "Owner customer not found.");

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

    private static async Task EnsureOwnerExistsAsync(IOrganizationDbContext context, long? ownerCustomerId, CancellationToken ct)
    {
        if (ownerCustomerId.HasValue &&
            !await context.Customers.AnyAsync(c => c.Id == ownerCustomerId.Value, ct))
            throw new EntityNotFoundException("GRAVE_OWNER_NOT_FOUND", "Owner customer not found.");
    }

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
