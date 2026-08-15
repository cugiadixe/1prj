using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Tags.DTOs;
using PTKD.Domain.Entities;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Tags.Services;

public class TagService : ITagService
{
    private static readonly HashSet<string> AllowedTypes = new(new[] { Tag.TypeCustomer, Tag.TypeGrave });

    // Bảng màu preset Ant Design để tự gán màu ổn định theo tên khi tạo thẻ mới.
    private static readonly string[] Palette =
        { "magenta", "red", "volcano", "orange", "gold", "lime", "green", "cyan", "blue", "geekblue", "purple" };

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;
    private readonly IPermissionEvaluator _permissionEvaluator;

    public TagService(
        IOrganizationDbContextFactory dbContextFactory,
        ITransactionalAuditWriter auditWriter,
        IPermissionEvaluator permissionEvaluator)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
        _permissionEvaluator = permissionEvaluator;
    }

    public async Task<IReadOnlyList<TagDto>> ListTagsAsync(string tagType, bool includeInactive, CancellationToken ct = default)
    {
        ValidateType(tagType);
        await using var context = _dbContextFactory.CreateDbContext();

        var query = context.Tags.AsNoTracking().Where(t => t.TagType == tagType);
        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        var usageByTag = tagType == Tag.TypeCustomer
            ? context.CustomerTags.GroupBy(x => x.TagId).Select(g => new { TagId = g.Key, Count = g.Count() })
            : context.GraveTags.GroupBy(x => x.TagId).Select(g => new { TagId = g.Key, Count = g.Count() });

        var tags = await query
            .GroupJoin(usageByTag, t => t.Id, u => u.TagId, (t, us) => new { t, count = us.Select(x => x.Count).FirstOrDefault() })
            .OrderBy(x => x.t.Name)
            .Select(x => new TagDto
            {
                Id = x.t.Id,
                TagType = x.t.TagType,
                Name = x.t.Name,
                Color = x.t.Color,
                IsActive = x.t.IsActive,
                UsageCount = x.count,
                RowVersion = Convert.ToBase64String(x.t.RowVersion)
            })
            .ToListAsync(ct);

        return tags;
    }

    public async Task<TagDto> CreateTagAsync(CreateTagRequest request, long actorUserId, CancellationToken ct = default)
    {
        ValidateType(request.TagType);
        var name = NormalizeName(request.Name);
        if (name == null)
            throw new BusinessRuleValidationException("TAG_NAME_REQUIRED", "Tag name is required.");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            if (await context.Tags.AnyAsync(t => t.TagType == request.TagType && t.Name == name, ct))
                throw new BusinessRuleValidationException("TAG_DUPLICATE", "A tag with this name already exists for this type.");

            var color = string.IsNullOrWhiteSpace(request.Color) ? ColorForName(name) : request.Color!.Trim();
            var tag = new Tag(request.TagType, name, color, actorUserId);
            context.Tags.Add(tag);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "TAG_CREATE", tag.Id, new { tag.TagType, tag.Name }, actorUserId, ct);
            await transaction.CommitAsync(ct);

            return MapToDto(tag, 0);
        });
    }

    public async Task<TagDto> UpdateTagAsync(long id, UpdateTagRequest request, long actorUserId, CancellationToken ct = default)
    {
        var name = NormalizeName(request.Name);
        if (name == null)
            throw new BusinessRuleValidationException("TAG_NAME_REQUIRED", "Tag name is required.");
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (tag == null)
                throw new EntityNotFoundException("TAG_NOT_FOUND", "Tag not found.");
            if (!tag.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("TAG_INVALID_ROW_VERSION", "The tag has been modified by another process.");

            if (name != tag.Name &&
                await context.Tags.AnyAsync(t => t.TagType == tag.TagType && t.Name == name && t.Id != id, ct))
                throw new BusinessRuleValidationException("TAG_DUPLICATE", "A tag with this name already exists for this type.");

            var color = string.IsNullOrWhiteSpace(request.Color) ? tag.Color : request.Color!.Trim();
            tag.Update(name, color, request.IsActive, actorUserId);

            try
            {
                await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("TAG_INVALID_ROW_VERSION", "The tag has been modified by another process.");
            }

            await WriteAuditAsync(context, "TAG_UPDATE", tag.Id, new { tag.Name, tag.IsActive }, actorUserId, ct);
            await transaction.CommitAsync(ct);

            var count = await CountUsageAsync(context, tag, ct);
            return MapToDto(tag, count);
        });
    }

    public async Task DeactivateTagAsync(long id, long actorUserId, CancellationToken ct = default)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var tag = await context.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (tag == null)
                throw new EntityNotFoundException("TAG_NOT_FOUND", "Tag not found.");

            // Gỡ mọi liên kết rồi ẩn thẻ khỏi danh mục (giữ bản ghi thẻ để không mất lịch sử).
            if (tag.TagType == Tag.TypeCustomer)
                context.CustomerTags.RemoveRange(context.CustomerTags.Where(x => x.TagId == id));
            else
                context.GraveTags.RemoveRange(context.GraveTags.Where(x => x.TagId == id));

            tag.Update(tag.Name, tag.Color, false, actorUserId);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "TAG_DEACTIVATE", tag.Id, new { tag.TagType, tag.Name }, actorUserId, ct);
            await transaction.CommitAsync(ct);
        });
    }

    public Task<IReadOnlyList<TagDto>> SetCustomerTagsAsync(long customerId, SetEntityTagsRequest request, long actorUserId, CancellationToken ct = default)
        => SetEntityTagsAsync(customerId, Tag.TypeCustomer, request, actorUserId, ct);

    public Task<IReadOnlyList<TagDto>> SetGraveTagsAsync(long graveId, SetEntityTagsRequest request, long actorUserId, CancellationToken ct = default)
        => SetEntityTagsAsync(graveId, Tag.TypeGrave, request, actorUserId, ct);

    private async Task<IReadOnlyList<TagDto>> SetEntityTagsAsync(long entityId, string tagType, SetEntityTagsRequest request, long actorUserId, CancellationToken ct)
    {
        var newNames = (request.NewTagNames ?? Array.Empty<string>())
            .Select(NormalizeName).Where(n => n != null).Select(n => n!)
            .GroupBy(n => n.ToLowerInvariant()).Select(g => g.First())
            .ToList();
        var providedIds = (request.TagIds ?? Array.Empty<long>()).Distinct().ToList();

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            await EnsureEntityExistsAsync(context, entityId, tagType, ct);

            // Đây là thao tác ĐẶT LẠI TOÀN BỘ, không phải thêm — nên thiếu chốt công ty thì một
            // lệnh gọi xoá sạch thẻ phân loại khách hàng của công ty khác chỉ bằng cách đoán id.
            // Mộ chưa có chiều công ty trong dữ liệu nên chỉ chốt được ở phía khách hàng; phần
            // mộ sẽ siết cùng đợt dựng thực thể nghĩa trang.
            if (tagType == Tag.TypeCustomer)
            {
                var scope = await _permissionEvaluator.ResolveAsync(actorUserId, "TAG_MANAGE", ct);
                await CustomerCompanyScope.EnsureCustomerAccessibleAsync(
                    context, entityId, scope, "TAG_COMPANY_FORBIDDEN", ct);
            }

            // 1) Xác thực các tagId truyền vào (đúng loại + đang hoạt động)
            var validIds = new HashSet<long>();
            if (providedIds.Count > 0)
            {
                var found = await context.Tags
                    .Where(t => providedIds.Contains(t.Id) && t.TagType == tagType && t.IsActive)
                    .Select(t => t.Id).ToListAsync(ct);
                foreach (var idv in found) validIds.Add(idv);
                if (found.Count != providedIds.Count)
                    throw new BusinessRuleValidationException("TAG_INVALID_ID", "One or more tags are invalid for this type.");
            }

            // 2) Với tên thẻ mới: tìm thẻ sẵn có (bỏ qua hoa/thường) hoặc tạo mới
            foreach (var name in newNames)
            {
                var existing = await context.Tags.FirstOrDefaultAsync(t => t.TagType == tagType && t.Name == name, ct);
                if (existing != null)
                {
                    if (!existing.IsActive) { existing.Update(existing.Name, existing.Color, true, actorUserId); }
                    validIds.Add(existing.Id);
                }
                else
                {
                    var tag = new Tag(tagType, name, ColorForName(name), actorUserId);
                    context.Tags.Add(tag);
                    await context.SaveChangesAsync(ct);
                    validIds.Add(tag.Id);
                }
            }

            // 3) Diff với tập hiện tại rồi áp (tách riêng theo loại để type an toàn)
            List<long> toAdd, toRemove;
            if (tagType == Tag.TypeCustomer)
            {
                var current = await context.CustomerTags.Where(x => x.CustomerId == entityId).ToListAsync(ct);
                var currentIds = current.Select(x => x.TagId).ToHashSet();
                toAdd = validIds.Where(x => !currentIds.Contains(x)).ToList();
                toRemove = currentIds.Where(x => !validIds.Contains(x)).ToList();
                context.CustomerTags.RemoveRange(current.Where(x => toRemove.Contains(x.TagId)));
                foreach (var tid in toAdd) context.CustomerTags.Add(new CustomerTag(entityId, tid, actorUserId));
            }
            else
            {
                var current = await context.GraveTags.Where(x => x.GraveId == entityId).ToListAsync(ct);
                var currentIds = current.Select(x => x.TagId).ToHashSet();
                toAdd = validIds.Where(x => !currentIds.Contains(x)).ToList();
                toRemove = currentIds.Where(x => !validIds.Contains(x)).ToList();
                context.GraveTags.RemoveRange(current.Where(x => toRemove.Contains(x.TagId)));
                foreach (var tid in toAdd) context.GraveTags.Add(new GraveTag(entityId, tid, actorUserId));
            }

            await context.SaveChangesAsync(ct);

            var entityType = tagType == Tag.TypeCustomer ? "Customer" : "Grave";
            await WriteAuditAsync(context, "ENTITY_TAGS_SET", entityId,
                new { entityType, added = toAdd.Count, removed = toRemove.Count, total = validIds.Count }, actorUserId, ct);
            await transaction.CommitAsync(ct);

            // 4) Trả tập thẻ kết quả
            return (IReadOnlyList<TagDto>)await context.Tags.AsNoTracking()
                .Where(t => validIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .Select(t => new TagDto
                {
                    Id = t.Id, TagType = t.TagType, Name = t.Name, Color = t.Color,
                    IsActive = t.IsActive, RowVersion = Convert.ToBase64String(t.RowVersion)
                })
                .ToListAsync(ct);
        });
    }

    private static async Task EnsureEntityExistsAsync(IOrganizationDbContext context, long entityId, string tagType, CancellationToken ct)
    {
        var exists = tagType == Tag.TypeCustomer
            ? await context.Customers.AnyAsync(c => c.Id == entityId, ct)
            : await context.Graves.AnyAsync(g => g.Id == entityId, ct);
        if (!exists)
            throw new EntityNotFoundException(tagType == Tag.TypeCustomer ? "CUS_CUSTOMER_NOT_FOUND" : "GRAVE_NOT_FOUND", "Entity not found.");
    }

    private static async Task<int> CountUsageAsync(IOrganizationDbContext context, Tag tag, CancellationToken ct)
        => tag.TagType == Tag.TypeCustomer
            ? await context.CustomerTags.CountAsync(x => x.TagId == tag.Id, ct)
            : await context.GraveTags.CountAsync(x => x.TagId == tag.Id, ct);

    private async Task WriteAuditAsync(IOrganizationDbContext context, string eventCode, long entityId, object afterState, long actorUserId, CancellationToken ct)
    {
        var audit = new SecurityAuditEventRecord
        {
            EventCode = eventCode,
            EntityType = "Tag",
            EntityId = entityId.ToString(),
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            AfterStateJson = JsonSerializer.Serialize(afterState)
        };
        audit.ThrowIfContainsSensitiveData();
        await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);
    }

    private static void ValidateType(string tagType)
    {
        if (!AllowedTypes.Contains(tagType))
            throw new BusinessRuleValidationException("TAG_INVALID_TYPE", "Invalid tag type. Allowed: CUSTOMER, GRAVE.");
    }

    private static string? NormalizeName(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var name = raw.Trim().TrimStart('#').Trim();
        if (name.Length == 0) return null;
        return name.Length > 50 ? name.Substring(0, 50) : name;
    }

    private static string ColorForName(string name)
    {
        int hash = 0;
        foreach (var ch in name.ToLowerInvariant()) hash = unchecked(hash * 31 + ch);
        return Palette[Math.Abs(hash) % Palette.Length];
    }

    private static TagDto MapToDto(Tag tag, int usageCount) => new()
    {
        Id = tag.Id,
        TagType = tag.TagType,
        Name = tag.Name,
        Color = tag.Color,
        IsActive = tag.IsActive,
        UsageCount = usageCount,
        RowVersion = Convert.ToBase64String(tag.RowVersion)
    };
}
