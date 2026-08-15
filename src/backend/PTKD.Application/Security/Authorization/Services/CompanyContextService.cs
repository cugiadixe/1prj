using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Security.Authorization.Services;

/// <inheritdoc cref="ICompanyContextService"/>
public class CompanyContextService : ICompanyContextService
{
    private readonly IAuthorizationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public CompanyContextService(IAuthorizationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<long>> GetMyCompanyIdsAsync(
        long userId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return await _dbContext.UserCompanyAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                        && a.AssignmentStatus == "ACTIVE"
                        && a.EffectiveFrom <= now
                        && (a.EffectiveTo == null || a.EffectiveTo > now))
            .Select(a => a.CompanyId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsMemberOfAsync(
        long userId,
        long companyId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return await _dbContext.UserCompanyAssignments
            .AsNoTracking()
            .AnyAsync(a => a.UserId == userId
                           && a.CompanyId == companyId
                           && a.AssignmentStatus == "ACTIVE"
                           && a.EffectiveFrom <= now
                           && (a.EffectiveTo == null || a.EffectiveTo > now),
                cancellationToken);
    }
}
