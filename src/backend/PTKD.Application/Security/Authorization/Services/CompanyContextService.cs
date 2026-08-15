using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Security.Authorization.Services;

/// <inheritdoc cref="ICompanyContextService"/>
public class CompanyContextService : ICompanyContextService
{
    private readonly IAuthorizationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ICompanyHierarchyService _companyHierarchy;

    public CompanyContextService(
        IAuthorizationDbContext dbContext,
        TimeProvider timeProvider,
        ICompanyHierarchyService companyHierarchy)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _companyHierarchy = companyHierarchy;
    }

    public async Task<IReadOnlyList<long>> GetMyCompanyIdsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var assigned = await _dbContext.UserCompanyAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                        && a.AssignmentStatus == "ACTIVE"
                        && a.EffectiveFrom <= now
                        && (a.EffectiveTo == null || a.EffectiveTo > now))
            .Select(a => a.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (assigned.Count == 0)
            return assigned;

        // Gán vào công ty MẸ (tập đoàn) thì thành viên của cả nhánh con — nhân viên tập đoàn
        // xem/thao tác được dữ liệu các công ty con.
        var expanded = await _companyHierarchy.ExpandWithDescendantsAsync(assigned, cancellationToken);
        return expanded.ToList();
    }

    public async Task<bool> IsMemberOfAsync(
        long userId,
        long companyId,
        CancellationToken cancellationToken = default)
    {
        // Đi qua tập đã nở theo cây: người gán ở công ty mẹ được coi là thành viên công ty con,
        // nên tự khai X-Company-Id của một công ty con hợp lệ. Nếu tách riêng một truy vấn AnyAsync
        // thì lại quên mất nhánh cây — đúng lỗi "hai chỗ trả lời lệch nhau" mà mô hình này tránh.
        var myCompanies = await GetMyCompanyIdsAsync(userId, cancellationToken);
        return myCompanies.Contains(companyId);
    }
}
