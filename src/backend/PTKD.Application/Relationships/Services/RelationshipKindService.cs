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
using PTKD.Domain.Entities;

namespace PTKD.Application.Relationships.Services;

public class RelationshipKindService : IRelationshipKindService
{
    // Loại hệ thống — engine suy diễn tham chiếu bằng MÃ (literal) hoặc bảng tra 2 bậc; không cho xoá.
    private static readonly HashSet<string> CoreCodes = new()
    {
        RelationshipKind.Parent, RelationshipKind.Child, RelationshipKind.Spouse,
        RelationshipKind.Sibling, RelationshipKind.SiblingOlder, RelationshipKind.SiblingYounger,
        RelationshipKind.GrandparentPaternal, RelationshipKind.GrandparentMaternal,
        RelationshipKind.GrandchildPaternal, RelationshipKind.GrandchildMaternal, RelationshipKind.Other,
    };

    private readonly IOrganizationDbContextFactory _dbContextFactory;
    private readonly ITransactionalAuditWriter _auditWriter;

    public RelationshipKindService(IOrganizationDbContextFactory dbContextFactory, ITransactionalAuditWriter auditWriter)
    {
        _dbContextFactory = dbContextFactory;
        _auditWriter = auditWriter;
    }

    public async Task<IReadOnlyList<RelationshipKindDetailDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var kinds = await context.RelationshipKinds.AsNoTracking().OrderBy(k => k.SortOrder).ToListAsync(ct);
        var byCode = kinds.ToDictionary(k => k.KindCode);

        // Mã đang bị tham chiếu (không xoá được): cạnh quan hệ + bảng tra suy diễn 2 bậc.
        var usedInRel = (await context.CustomerRelationships.AsNoTracking()
            .Select(r => r.RelationKind).Distinct().ToListAsync(ct)).ToHashSet();
        var comps = await context.KinshipCompositions.AsNoTracking()
            .Select(c => new { c.KindA, c.KindB, c.ResultKind }).ToListAsync(ct);
        var usedInComp = comps.SelectMany(c => new[] { c.KindA, c.KindB, c.ResultKind }).ToHashSet();

        bool Referenced(string code) => usedInRel.Contains(code) || usedInComp.Contains(code);

        return kinds.Select(k =>
        {
            var isCore = CoreCodes.Contains(k.KindCode);
            // Xoá là xoá cả cặp (loại + nghịch đảo), nên bị tham chiếu ở BẤT KỲ đầu nào là chặn.
            var referenced = Referenced(k.KindCode) || (k.InverseCode != k.KindCode && Referenced(k.InverseCode));
            return new RelationshipKindDetailDto
            {
                KindCode = k.KindCode,
                LabelMale = k.LabelMale, LabelFemale = k.LabelFemale, LabelNeutral = k.LabelNeutral,
                InverseCode = k.InverseCode,
                InverseLabelNeutral = byCode.TryGetValue(k.InverseCode, out var inv) ? inv.LabelNeutral : null,
                IsSymmetric = k.IsSymmetric,
                SortOrder = k.SortOrder,
                IsCore = isCore,
                Deletable = !isCore && !referenced,
            };
        }).ToList();
    }

    public async Task<RelationshipKindDetailDto> CreateAsync(CreateRelationshipKindRequest request, long actorUserId, CancellationToken ct = default)
    {
        ValidateSide(request.SideA, "SideA");
        if (!request.IsSymmetric)
        {
            if (request.SideB == null) throw new BusinessRuleValidationException("REL_KIND_NEED_SIDE_B", "Quan hệ bất đối xứng cần cả hai vế.");
            ValidateSide(request.SideB, "SideB");
        }

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var codeA = await GenerateCodeAsync(context, ct);
            RelationshipKind kindA;
            if (request.IsSymmetric)
            {
                // Tự nghịch đảo.
                kindA = new RelationshipKind(codeA, request.SideA.LabelMale, request.SideA.LabelFemale,
                    request.SideA.LabelNeutral, codeA, isSymmetric: true, request.SortOrder);
                context.RelationshipKinds.Add(kindA);
            }
            else
            {
                var codeB = await GenerateCodeAsync(context, ct, exclude: codeA);
                kindA = new RelationshipKind(codeA, request.SideA.LabelMale, request.SideA.LabelFemale,
                    request.SideA.LabelNeutral, codeB, isSymmetric: false, request.SortOrder);
                var kindB = new RelationshipKind(codeB, request.SideB!.LabelMale, request.SideB.LabelFemale,
                    request.SideB.LabelNeutral, codeA, isSymmetric: false, request.SortOrder + 1);
                context.RelationshipKinds.Add(kindA);
                context.RelationshipKinds.Add(kindB);
            }
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "RELATIONSHIP_KIND_CREATE", codeA,
                new { codeA, isSymmetric = request.IsSymmetric, request.SideA.LabelNeutral }, actorUserId, ct);

            await transaction.CommitAsync(ct);

            return MapToDetail(kindA, request.IsSymmetric ? request.SideA.LabelNeutral : request.SideB!.LabelNeutral, isCore: false, deletable: true);
        });
    }

    public async Task UpdateAsync(string kindCode, UpdateRelationshipKindRequest request, long actorUserId, CancellationToken ct = default)
    {
        ValidateSide(new RelationshipKindSideInput { LabelMale = request.LabelMale, LabelFemale = request.LabelFemale, LabelNeutral = request.LabelNeutral }, "labels");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var kind = await context.RelationshipKinds.FirstOrDefaultAsync(k => k.KindCode == kindCode, ct);
            if (kind == null) throw new EntityNotFoundException("REL_KIND_NOT_FOUND", "Không tìm thấy loại quan hệ.");

            kind.Update(request.LabelMale, request.LabelFemale, request.LabelNeutral, request.SortOrder);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "RELATIONSHIP_KIND_UPDATE", kindCode,
                new { kindCode, request.LabelNeutral }, actorUserId, ct);

            await transaction.CommitAsync(ct);
        });
    }

    public async Task DeleteAsync(string kindCode, long actorUserId, CancellationToken ct = default)
    {
        if (CoreCodes.Contains(kindCode))
            throw new BusinessRuleValidationException("REL_KIND_CORE", "Loại quan hệ hệ thống, không xoá được.");

        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

            var kind = await context.RelationshipKinds.FirstOrDefaultAsync(k => k.KindCode == kindCode, ct);
            if (kind == null) throw new EntityNotFoundException("REL_KIND_NOT_FOUND", "Không tìm thấy loại quan hệ.");

            var codes = new List<string> { kind.KindCode };
            if (kind.InverseCode != kind.KindCode) codes.Add(kind.InverseCode);

            // Chặn nếu bất kỳ mã nào (loại hoặc nghịch đảo) đang được dùng.
            if (await context.CustomerRelationships.AnyAsync(r => codes.Contains(r.RelationKind), ct))
                throw new BusinessRuleValidationException("REL_KIND_IN_USE", "Loại quan hệ đang được dùng ở một số khách hàng, không xoá được.");
            if (await context.KinshipCompositions.AnyAsync(c => codes.Contains(c.KindA) || codes.Contains(c.KindB) || codes.Contains(c.ResultKind), ct))
                throw new BusinessRuleValidationException("REL_KIND_IN_COMPOSITION", "Loại quan hệ nằm trong quy tắc suy diễn, không xoá được.");

            var toRemove = await context.RelationshipKinds.Where(k => codes.Contains(k.KindCode)).ToListAsync(ct);
            context.RelationshipKinds.RemoveRange(toRemove);
            await context.SaveChangesAsync(ct);

            await WriteAuditAsync(context, "RELATIONSHIP_KIND_DELETE", kindCode, new { codes }, actorUserId, ct);

            await transaction.CommitAsync(ct);
        });
    }

    private static void ValidateSide(RelationshipKindSideInput s, string name)
    {
        if (s == null || string.IsNullOrWhiteSpace(s.LabelMale) || string.IsNullOrWhiteSpace(s.LabelFemale) || string.IsNullOrWhiteSpace(s.LabelNeutral))
            throw new BusinessRuleValidationException("REL_KIND_LABEL_REQUIRED", $"Thiếu nhãn ({name}): cần đủ nhãn Nam/Nữ/Chung.");
    }

    // Mã máy tự sinh, không cho người dùng thấy; ≤ 24 ký tự (giới hạn cột).
    private static async Task<string> GenerateCodeAsync(IOrganizationDbContext context, CancellationToken ct, string? exclude = null)
    {
        for (var i = 0; i < 10; i++)
        {
            var code = "CUSTOM_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant();
            if (code == exclude) continue;
            if (!await context.RelationshipKinds.AnyAsync(k => k.KindCode == code, ct))
                return code;
        }
        throw new BusinessRuleValidationException("REL_KIND_CODE_GEN", "Không sinh được mã loại quan hệ, thử lại.");
    }

    private static RelationshipKindDetailDto MapToDetail(RelationshipKind k, string? inverseLabelNeutral, bool isCore, bool deletable) => new()
    {
        KindCode = k.KindCode,
        LabelMale = k.LabelMale, LabelFemale = k.LabelFemale, LabelNeutral = k.LabelNeutral,
        InverseCode = k.InverseCode, InverseLabelNeutral = inverseLabelNeutral,
        IsSymmetric = k.IsSymmetric, SortOrder = k.SortOrder, IsCore = isCore, Deletable = deletable,
    };

    private async Task WriteAuditAsync(IOrganizationDbContext context, string eventCode, string entityId, object after, long actorUserId, CancellationToken ct)
    {
        var audit = new SecurityAuditEventRecord
        {
            EventCode = eventCode,
            EntityType = "RelationshipKind",
            EntityId = entityId,
            Outcome = "SUCCESS",
            CorrelationId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            AfterStateJson = JsonSerializer.Serialize(after),
        };
        audit.ThrowIfContainsSensitiveData();
        await _auditWriter.WriteAsync(audit, context.GetDbConnection(), context.GetCurrentDbTransaction()!, ct);
    }
}
