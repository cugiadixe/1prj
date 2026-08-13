using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Application.Relationships.Services;

public class RelationshipDerivationService : IRelationshipDerivationService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public RelationshipDerivationService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<DerivedRelationship>> DeriveOwnerToOccupantsAsync(
        long ownerCustomerId, IReadOnlyList<long> occupantCustomerIds, CancellationToken ct = default)
    {
        var results = new List<DerivedRelationship>();
        if (occupantCustomerIds.Count == 0) return results;

        await using var ctx = _dbContextFactory.CreateDbContext();

        // Bảng tham chiếu (nhỏ) — nạp toàn bộ vào bộ nhớ
        var kinds = await ctx.RelationshipKinds.AsNoTracking().ToDictionaryAsync(k => k.KindCode, ct);
        var comps = (await ctx.KinshipCompositions.AsNoTracking().ToListAsync(ct))
            .ToDictionary(c => (c.KindA, c.KindB, c.PivotGender));

        // Giới tính + ngày sinh của những người liên quan
        var personIds = occupantCustomerIds.Append(ownerCustomerId).Distinct().ToList();
        var people = await ctx.Customers.AsNoTracking()
            .Where(c => personIds.Contains(c.Id))
            .Select(c => new PersonInfo(c.Id, c.Profile.Gender, c.Profile.Dob))
            .ToDictionaryAsync(p => p.Id, ct);

        // Cạnh trực tiếp chủ → cốt
        var directs = await ctx.CustomerRelationships.AsNoTracking()
            .Where(r => r.FromCustomerId == ownerCustomerId && occupantCustomerIds.Contains(r.ToCustomerId))
            .Select(r => new { r.ToCustomerId, r.RelationKind, r.NeedsConfirmation })
            .ToDictionaryAsync(r => r.ToCustomerId, r => (r.RelationKind, r.NeedsConfirmation), ct);

        // Ghép 2 bậc cho các cốt chưa có cạnh trực tiếp
        var missing = occupantCustomerIds.Where(id => !directs.ContainsKey(id)).ToList();
        var twoHop = new Dictionary<long, (string kind, bool needs)>();
        if (missing.Count > 0)
        {
            var ownerOut = await ctx.CustomerRelationships.AsNoTracking()
                .Where(r => r.FromCustomerId == ownerCustomerId)
                .Select(r => new { r.ToCustomerId, r.RelationKind })
                .ToListAsync(ct);
            var ownerOutMap = ownerOut
                .GroupBy(o => o.ToCustomerId)
                .ToDictionary(g => g.Key, g => g.First().RelationKind);
            var pivotIds = ownerOutMap.Keys.ToList();

            if (pivotIds.Count > 0)
            {
                var pivotGender = await ctx.Customers.AsNoTracking()
                    .Where(c => pivotIds.Contains(c.Id))
                    .Select(c => new { c.Id, c.Profile.Gender })
                    .ToDictionaryAsync(x => x.Id, x => x.Gender, ct);

                var pivotToOcc = await ctx.CustomerRelationships.AsNoTracking()
                    .Where(r => pivotIds.Contains(r.FromCustomerId) && missing.Contains(r.ToCustomerId))
                    .Select(r => new { r.FromCustomerId, r.ToCustomerId, r.RelationKind })
                    .ToListAsync(ct);
                var p2o = pivotToOcc.ToDictionary(x => (x.FromCustomerId, x.ToCustomerId), x => x.RelationKind);

                foreach (var occ in missing)
                {
                    foreach (var pv in pivotIds)
                    {
                        if (!p2o.TryGetValue((pv, occ), out var kindB)) continue;
                        var kindA = ownerOutMap[pv];
                        var pg = pivotGender.TryGetValue(pv, out var g) ? g : null;

                        if ((pg != null && comps.TryGetValue((kindA, kindB, pg), out var comp)) ||
                            comps.TryGetValue((kindA, kindB, KinshipComposition.PivotAny), out comp))
                        {
                            twoHop[occ] = (comp.ResultKind, comp.NeedsConfirmation);
                            break;
                        }
                    }
                }
            }
        }

        PersonInfo? owner = people.TryGetValue(ownerCustomerId, out var ow) ? ow : null;

        foreach (var occId in occupantCustomerIds)
        {
            string kind;
            bool needs;
            if (directs.TryGetValue(occId, out var de)) { kind = de.RelationKind; needs = de.NeedsConfirmation; }
            else if (twoHop.TryGetValue(occId, out var th)) { kind = th.kind; needs = th.needs; }
            else { kind = RelationshipKind.Other; needs = true; }

            PersonInfo? occ = people.TryGetValue(occId, out var oc) ? oc : null;
            var inverseKind = kinds.TryGetValue(kind, out var k) ? k.InverseCode : RelationshipKind.Other;

            // owner_relationship = "chủ mộ LÀ GÌ của người mất" → quan hệ NGHỊCH ĐẢO, theo giới tính CHỦ
            var ownerLabel = ResolveLabel(kinds, inverseKind, owner?.Gender, owner?.Dob, occ?.Dob);
            // deceased_relationship = "người mất LÀ GÌ của chủ mộ" → quan hệ THUẬN (kind), theo giới tính CỐT
            var occLabel = ResolveLabel(kinds, kind, occ?.Gender, occ?.Dob, owner?.Dob);

            results.Add(new DerivedRelationship(occId, kind, ownerLabel, occLabel, needs));
        }

        return results;
    }

    /// <summary>
    /// Nhãn tiếng Việt cho quan hệ, có phân giải Anh/Chị (lớn tuổi) vs Em (nhỏ tuổi)
    /// cho SIBLING theo ngày sinh của đối tượng so với người tham chiếu.
    /// </summary>
    private static string ResolveLabel(
        IReadOnlyDictionary<string, RelationshipKind> kinds,
        string kindCode, string? targetGender, DateTime? targetDob, DateTime? referenceDob)
    {
        if (kindCode == RelationshipKind.Sibling)
        {
            if (targetDob.HasValue && referenceDob.HasValue && targetDob.Value != referenceDob.Value)
            {
                kindCode = targetDob.Value < referenceDob.Value
                    ? RelationshipKind.SiblingOlder      // sinh trước = lớn tuổi hơn = Anh/Chị
                    : RelationshipKind.SiblingYounger;   // Em
            }
            else
            {
                // Thiếu ngày sinh (hoặc trùng ngày sinh) ⇒ không xác định được anh/chị vs em.
                // Trả nhãn TRUNG TÍNH "Anh/Chị/Em" thay vì nhãn gộp theo giới tính, và để
                // needs_confirmation của cạnh quan hệ nhắc người xác nhận cụ thể sau.
                return kinds.TryGetValue(RelationshipKind.Sibling, out var s) ? s.LabelNeutral : "Anh/Chị/Em";
            }
        }

        return kinds.TryGetValue(kindCode, out var kind) ? kind.LabelFor(targetGender) : "Người thân";
    }

    private readonly record struct PersonInfo(long Id, string? Gender, DateTime? Dob);
}
