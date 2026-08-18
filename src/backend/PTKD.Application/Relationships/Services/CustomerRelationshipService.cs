using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Relationships.DTOs;
using PTKD.Application.Security.Audit;
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Application.Relationships.Services;

public class CustomerRelationshipService : ICustomerRelationshipService
{
    private const string ViewPermission = "CUSTOMER_VIEW_BASIC";
    private const string ManagePermission = "CUSTOMER_RELATIONSHIP_MANAGE";

    // Nhãn hiển thị Anh/Chị vs Em suy theo TUỔI lúc xem, không cho khai trực tiếp.
    private static readonly HashSet<string> NonDeclarableKinds =
        new() { RelationshipKind.SiblingOlder, RelationshipKind.SiblingYounger };

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly ITransactionalAuditWriter _auditWriter;

    public CustomerRelationshipService(
        IOrganizationDbContextFactory dbContextFactory,
        IPermissionEvaluator permissionEvaluator,
        ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _permissionEvaluator = permissionEvaluator;
        _auditWriter = auditWriter;
    }

    public async Task<IReadOnlyList<RelationshipKindDto>> GetKindsAsync(CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var kinds = await context.RelationshipKinds.AsNoTracking()
            .OrderBy(k => k.SortOrder)
            .ToListAsync(ct);

        return kinds
            .Where(k => !NonDeclarableKinds.Contains(k.KindCode))
            .Select(k => new RelationshipKindDto
            {
                KindCode = k.KindCode,
                Label = k.LabelNeutral,
                InverseCode = k.InverseCode,
                IsSymmetric = k.IsSymmetric,
                SortOrder = k.SortOrder,
            })
            .ToList();
    }

    public async Task<RelationshipPagedResult<RelationshipListItemDto>> SearchAllAsync(RelationshipSearchRequest request, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);

        var empty = new RelationshipPagedResult<RelationshipListItemDto>
        {
            Items = Array.Empty<RelationshipListItemDto>(), TotalCount = 0, Page = request.Page, PageSize = request.PageSize,
        };
        if (!scope.Granted) return empty;

        // Mỗi cặp một dòng (canonical from < to) để không hiện trùng cả hai chiều.
        var q = context.CustomerRelationships.AsNoTracking().Where(r => r.FromCustomerId < r.ToCustomerId);

        if (!string.IsNullOrWhiteSpace(request.Kind))
        {
            var kind = request.Kind;
            // Khớp cả khi chiều canonical là <kind> HOẶC nghịch đảo của canonical là <kind> (cùng một loại quan hệ).
            q = q.Where(r => r.RelationKind == kind
                || context.RelationshipKinds.Any(k => k.KindCode == r.RelationKind && k.InverseCode == kind));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            q = q.Where(r =>
                context.Customers.Any(c => c.Id == r.FromCustomerId && (c.CustomerCode.Contains(term) || c.Profile.FullName.Contains(term)))
                || context.Customers.Any(c => c.Id == r.ToCustomerId && (c.CustomerCode.Contains(term) || c.Profile.FullName.Contains(term))));
        }

        // Phạm vi: chỉ hiện quan hệ mà CẢ HAI đầu người gọi được phép thấy (không lộ tên khách công ty khác).
        if (!scope.IsUnrestricted)
        {
            var allowed = scope.AllowedCompanyIds;
            q = q.Where(r =>
                context.CustomerCompanyContexts.Any(cc => cc.CustomerId == r.FromCustomerId && allowed.Contains(cc.CompanyId))
                && context.CustomerCompanyContexts.Any(cc => cc.CustomerId == r.ToCustomerId && allowed.Contains(cc.CompanyId)));
        }

        var total = await q.CountAsync(ct);
        var edges = await q.OrderByDescending(r => r.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);
        if (edges.Count == 0) { empty.TotalCount = total; return empty; }

        var ids = edges.SelectMany(e => new[] { e.FromCustomerId, e.ToCustomerId }).Distinct().ToList();
        var people = await context.Customers.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.CustomerCode, c.Profile.FullName, c.Profile.Gender })
            .ToDictionaryAsync(x => x.Id, ct);
        var kinds = await context.RelationshipKinds.AsNoTracking().ToDictionaryAsync(k => k.KindCode, ct);

        var items = new List<RelationshipListItemDto>();
        foreach (var e in edges)
        {
            if (!people.TryGetValue(e.FromCustomerId, out var from) || !people.TryGetValue(e.ToCustomerId, out var to))
                continue;
            var label = kinds.TryGetValue(e.RelationKind, out var k) ? k.LabelFor(to.Gender) : e.RelationKind;
            items.Add(new RelationshipListItemDto
            {
                Id = e.Id,
                FromCustomerId = e.FromCustomerId, FromCustomerCode = from.CustomerCode, FromCustomerName = from.FullName,
                ToCustomerId = e.ToCustomerId, ToCustomerCode = to.CustomerCode, ToCustomerName = to.FullName,
                RelationKind = e.RelationKind, RelationLabel = label,
                IsDerived = e.IsDerived, NeedsConfirmation = e.NeedsConfirmation, Note = e.Note,
            });
        }

        return new RelationshipPagedResult<RelationshipListItemDto>
        {
            Items = items.ToArray(), TotalCount = total, Page = request.Page, PageSize = request.PageSize,
        };
    }

    public async Task<IReadOnlyList<CustomerRelationshipDto>> GetForCustomerAsync(long customerId, long actorUserId, CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();

        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ViewPermission, ct);
        await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, customerId, scope, "CUS_REL_VIEW_FORBIDDEN_COMPANY", ct);

        var edges = await context.CustomerRelationships.AsNoTracking()
            .Where(r => r.FromCustomerId == customerId)
            .ToListAsync(ct);
        if (edges.Count == 0) return Array.Empty<CustomerRelationshipDto>();

        // Chốt chống lộ: chỉ hiện người thân người gọi cũng được phép thấy (không lộ khách công ty khác).
        var otherIds = edges.Select(e => e.ToCustomerId).Distinct().ToList();
        var accessible = await CustomerCompanyScope.FilterAccessibleCustomerIdsAsync(context, otherIds, scope, ct);

        var others = await context.Customers.AsNoTracking()
            .Where(c => otherIds.Contains(c.Id))
            .Select(c => new { c.Id, c.CustomerCode, c.Profile.FullName, c.Profile.Gender })
            .ToDictionaryAsync(x => x.Id, ct);
        var kinds = await context.RelationshipKinds.AsNoTracking().ToDictionaryAsync(k => k.KindCode, ct);

        var result = new List<CustomerRelationshipDto>();
        foreach (var e in edges)
        {
            if (!accessible.Contains(e.ToCustomerId)) continue;
            if (!others.TryGetValue(e.ToCustomerId, out var other)) continue;
            var label = kinds.TryGetValue(e.RelationKind, out var k) ? k.LabelFor(other.Gender) : e.RelationKind;

            result.Add(new CustomerRelationshipDto
            {
                Id = e.Id,
                FromCustomerId = e.FromCustomerId,
                OtherCustomerId = e.ToCustomerId,
                OtherCustomerCode = other.CustomerCode,
                OtherCustomerName = other.FullName,
                RelationKind = e.RelationKind,
                RelationLabel = label,
                IsDerived = e.IsDerived,
                NeedsConfirmation = e.NeedsConfirmation,
                Note = e.Note,
                RowVersion = Convert.ToBase64String(e.RowVersion),
            });
        }
        return result;
    }

    public async Task<CustomerRelationshipDto> CreateAsync(long customerId, CreateCustomerRelationshipRequest request, long actorUserId, CancellationToken ct = default)
    {
        if (request.OtherCustomerId == customerId)
            throw new BusinessRuleValidationException("CUS_REL_SELF", "Không thể khai quan hệ với chính khách hàng này.");

        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ManagePermission, ct);

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            // Cả hai khách phải trong phạm vi người thao tác — không cho nối một khách ngoài quyền.
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, customerId, scope, "CUS_REL_FORBIDDEN_COMPANY", ct);
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, request.OtherCustomerId, scope, "CUS_REL_OTHER_FORBIDDEN_COMPANY", ct);

            var kind = await context.RelationshipKinds.AsNoTracking()
                .FirstOrDefaultAsync(k => k.KindCode == request.RelationKind, ct);
            if (kind == null || NonDeclarableKinds.Contains(kind.KindCode))
                throw new BusinessRuleValidationException("CUS_REL_INVALID_KIND", "Loại quan hệ không hợp lệ.");

            if (!await context.Customers.AnyAsync(c => c.Id == request.OtherCustomerId, ct))
                throw new EntityNotFoundException("CUS_REL_OTHER_NOT_FOUND", "Không tìm thấy khách hàng người thân.");

            // Cạnh thuận: "người thân LÀ <kind> của khách này" (from=khách, to=người thân).
            await UpsertEdgeAsync(context, customerId, request.OtherCustomerId, kind.KindCode, request.Note, actorUserId, ct);
            // Cạnh nghịch đảo: "khách này LÀ <inverse> của người thân" (from=người thân, to=khách).
            await UpsertEdgeAsync(context, request.OtherCustomerId, customerId, kind.InverseCode, null, actorUserId, ct);

            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_RELATIONSHIP_SET",
                EntityType = "CustomerRelationship",
                EntityId = customerId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { fromCustomerId = customerId, toCustomerId = request.OtherCustomerId, relationKind = kind.KindCode }),
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);

            // Đọc lại cạnh thuận để trả về (kèm nhãn theo giới tính người thân).
            var edge = await context.CustomerRelationships.AsNoTracking()
                .FirstAsync(r => r.FromCustomerId == customerId && r.ToCustomerId == request.OtherCustomerId, ct);
            var other = await context.Customers.AsNoTracking()
                .Where(c => c.Id == request.OtherCustomerId)
                .Select(c => new { c.CustomerCode, c.Profile.FullName, c.Profile.Gender })
                .FirstAsync(ct);

            return new CustomerRelationshipDto
            {
                Id = edge.Id,
                FromCustomerId = customerId,
                OtherCustomerId = request.OtherCustomerId,
                OtherCustomerCode = other.CustomerCode,
                OtherCustomerName = other.FullName,
                RelationKind = edge.RelationKind,
                RelationLabel = kind.LabelFor(other.Gender),
                IsDerived = edge.IsDerived,
                NeedsConfirmation = edge.NeedsConfirmation,
                Note = edge.Note,
                RowVersion = Convert.ToBase64String(edge.RowVersion),
            };
        });
    }

    public async Task DeleteAsync(long customerId, long relationshipId, long actorUserId, CancellationToken ct = default)
    {
        var scope = await _permissionEvaluator.ResolveAsync(actorUserId, ManagePermission, ct);

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var edge = await context.CustomerRelationships
                .FirstOrDefaultAsync(r => r.Id == relationshipId && r.FromCustomerId == customerId, ct);
            if (edge == null)
                throw new EntityNotFoundException("CUS_REL_NOT_FOUND", "Không tìm thấy quan hệ.");

            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, customerId, scope, "CUS_REL_FORBIDDEN_COMPANY", ct);
            await CustomerCompanyScope.EnsureCustomerAccessibleAsync(context, edge.ToCustomerId, scope, "CUS_REL_OTHER_FORBIDDEN_COMPANY", ct);

            context.CustomerRelationships.Remove(edge);
            // Xoá luôn cạnh nghịch đảo nếu có.
            var inverse = await context.CustomerRelationships
                .FirstOrDefaultAsync(r => r.FromCustomerId == edge.ToCustomerId && r.ToCustomerId == customerId, ct);
            if (inverse != null)
                context.CustomerRelationships.Remove(inverse);

            await context.SaveChangesAsync(ct);

            var audit = new SecurityAuditEventRecord
            {
                EventCode = "CUSTOMER_RELATIONSHIP_REMOVE",
                EntityType = "CustomerRelationship",
                EntityId = customerId.ToString(),
                Outcome = "SUCCESS",
                CorrelationId = Guid.NewGuid(),
                ActorUserId = actorUserId,
                AfterStateJson = JsonSerializer.Serialize(new { fromCustomerId = customerId, toCustomerId = edge.ToCustomerId }),
            };
            audit.ThrowIfContainsSensitiveData();
            await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);

            await transaction.CommitAsync(ct);
        });
    }

    // Ghi cạnh (from→to = kind); nếu đã có cạnh cho cặp này thì phân loại lại (khai đè, hết suy diễn).
    private static async Task UpsertEdgeAsync(
        IOrganizationDbContext context, long fromId, long toId, string kind, string? note, long actorUserId, CancellationToken ct)
    {
        var existing = await context.CustomerRelationships
            .FirstOrDefaultAsync(r => r.FromCustomerId == fromId && r.ToCustomerId == toId, ct);
        if (existing != null)
        {
            existing.Reclassify(kind, isDerived: false, needsConfirmation: false, actorUserId);
        }
        else
        {
            var edge = new CustomerRelationship(fromId, toId, kind, isDerived: false, needsConfirmation: false, note, actorUserId);
            context.CustomerRelationships.Add(edge);
        }
    }
}
