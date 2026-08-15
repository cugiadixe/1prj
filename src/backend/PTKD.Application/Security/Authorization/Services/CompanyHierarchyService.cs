using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Security.Authorization.Services;

/// <inheritdoc cref="ICompanyHierarchyService"/>
public class CompanyHierarchyService : ICompanyHierarchyService
{
    private const string ChildrenMapCacheKey = "company-hierarchy:children-map";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IAuthorizationDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public CompanyHierarchyService(IAuthorizationDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<long>> ExpandWithDescendantsAsync(
        IEnumerable<long> companyIds,
        CancellationToken cancellationToken = default)
    {
        var seeds = companyIds as IReadOnlyCollection<long> ?? companyIds.ToList();
        if (seeds.Count == 0)
            return Array.Empty<long>();

        var childrenByParent = await GetChildrenMapAsync(cancellationToken);

        // Duyệt theo chiều rộng từ mỗi công ty gốc. HashSet vừa khử trùng lặp vừa là chốt chống
        // chu trình: nếu dữ liệu công ty lỡ có vòng (A là con của B, B là con của A) thì mỗi id
        // chỉ vào hàng đợi một lần nên không lặp vô hạn.
        var result = new HashSet<long>();
        var queue = new Queue<long>(seeds);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current))
                continue;

            if (childrenByParent.TryGetValue(current, out var children))
                foreach (var child in children)
                    queue.Enqueue(child);
        }

        return result;
    }

    /// <summary>
    /// Bản đồ cha → danh sách con TRỰC TIẾP, nạp một lần rồi cache. Cây công ty đổi rất hiếm
    /// (thêm/tách công ty là việc quản trị), nên cache 5 phút là đủ; đây cũng là lý do phép nở
    /// chỉ chạm CSDL lần đầu, các lần sau đều trong bộ nhớ.
    /// </summary>
    private async Task<IReadOnlyDictionary<long, IReadOnlyList<long>>> GetChildrenMapAsync(
        CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(ChildrenMapCacheKey, out IReadOnlyDictionary<long, IReadOnlyList<long>>? cached)
            && cached != null)
            return cached;

        var edges = await _dbContext.Companies
            .AsNoTracking()
            .Where(c => c.ParentCompanyId != null)
            .Select(c => new { c.Id, ParentId = c.ParentCompanyId!.Value })
            .ToListAsync(cancellationToken);

        var map = edges
            .GroupBy(e => e.ParentId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<long>)g.Select(e => e.Id).ToList());

        _cache.Set(ChildrenMapCacheKey, map, CacheTtl);
        return map;
    }
}
