using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Security.Authorization.Services;

public class PermissionEvaluator : IPermissionEvaluator
{
    private readonly IAuthorizationDbContext _dbContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionEvaluator> _logger;
    private readonly TimeProvider _timeProvider;

    public PermissionEvaluator(
        IAuthorizationDbContext dbContext,
        IMemoryCache cache,
        ILogger<PermissionEvaluator> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<bool> EvaluateAsync(
        long userId,
        string permissionCode,
        long? companyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policyState = await _dbContext.AuthorizationPolicyStates
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == 1, cancellationToken);

            long policyVersion = policyState?.PolicyVersion ?? 1;

            var cacheKey = $"perm:{userId}:{companyId?.ToString() ?? "null"}:{policyVersion}:{permissionCode}";

            if (_cache.TryGetValue(cacheKey, out bool cachedResult))
            {
                return cachedResult;
            }

            var result = await EvaluateInternalAsync(userId, permissionCode, companyId, cancellationToken);

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail-closed: Error evaluating permission {PermissionCode} for User {UserId}", permissionCode, userId);
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        long userId,
        long? companyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policyState = await _dbContext.AuthorizationPolicyStates
                .AsNoTracking()
                .SingleOrDefaultAsync(p => p.Id == 1, cancellationToken);

            long policyVersion = policyState?.PolicyVersion ?? 1;

            var cacheKey = $"perms:{userId}:{companyId?.ToString() ?? "null"}:{policyVersion}";

            if (_cache.TryGetValue(cacheKey, out IReadOnlyList<string>? cachedResult) && cachedResult != null)
            {
                return cachedResult;
            }

            var permissions = await GetEffectivePermissionsInternalAsync(userId, companyId, cancellationToken);
            var resultList = permissions.ToList();

            _cache.Set(cacheKey, resultList, TimeSpan.FromMinutes(5));

            return resultList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fail-closed: Error getting effective permissions for User {UserId}", userId);
            return Array.Empty<string>();
        }
    }

    private async Task<bool> EvaluateInternalAsync(
        long userId,
        string permissionCode,
        long? companyId,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // 1. Check if permission catalog item is active
        var permissionInfo = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.PermissionCode == permissionCode && p.IsActive)
            .Select(p => new { p.DataScope })
            .SingleOrDefaultAsync(cancellationToken);

        if (permissionInfo == null)
        {
            return false;
        }

        // Scope validation
        if (permissionInfo.DataScope == "COMPANY" && companyId == null)
        {
            return false;
        }
        if (permissionInfo.DataScope == "GLOBAL" && companyId != null)
        {
            return false;
        }

        // 2. Check Individual DENY first (DENY wins over everything)
        var hasDeny = await _dbContext.UserIndividualPermissions
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId
                        && p.PermissionCode == permissionCode
                        && p.AssignmentStatus == "ACTIVE"
                        && p.GrantType == "DENY"
                        && p.EffectiveFrom <= now
                        && (p.EffectiveTo == null || p.EffectiveTo > now)
                        && (p.ScopeType == "GLOBAL" || p.CompanyId == companyId),
                cancellationToken);

        if (hasDeny)
        {
            return false;
        }

        // 3. Check Admin Group Grants
        var hasAdminGroupGrant = await _dbContext.UserAdminGroupAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.AssignmentStatus == "ACTIVE"
                     && a.EffectiveFrom <= now
                     && (a.EffectiveTo == null || a.EffectiveTo > now)
                     && a.AdminGroup.IsActive
                     && (a.AdminGroup.ScopeType == "GLOBAL" || a.AdminGroup.CompanyId == companyId))
            .SelectMany(a => a.AdminGroup.Permissions)
            .AnyAsync(p => p.PermissionCode == permissionCode, cancellationToken);

        if (hasAdminGroupGrant)
        {
            return true;
        }

        // 4. Check Individual ALLOW
        var hasIndividualAllow = await _dbContext.UserIndividualPermissions
            .AsNoTracking()
            .AnyAsync(p => p.UserId == userId
                        && p.PermissionCode == permissionCode
                        && p.AssignmentStatus == "ACTIVE"
                        && p.GrantType == "ALLOW"
                        && p.EffectiveFrom <= now
                        && (p.EffectiveTo == null || p.EffectiveTo > now)
                        && (p.ScopeType == "GLOBAL" || p.CompanyId == companyId),
                cancellationToken);

        if (hasIndividualAllow)
        {
            return true;
        }

        // 5. Check Role Grants
        var hasRoleGrant = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.AssignmentStatus == "ACTIVE"
                     && a.EffectiveFrom <= now
                     && (a.EffectiveTo == null || a.EffectiveTo > now)
                     && a.Role.IsActive
                     && (a.Role.ScopeType == "GLOBAL" || a.Role.CompanyId == companyId))
            .SelectMany(a => a.Role.Permissions)
            .AnyAsync(p => p.PermissionCode == permissionCode, cancellationToken);

        if (hasRoleGrant)
        {
            return true;
        }

        // 6. Check Department Baseline Grants
        var activeDepts = await _dbContext.UserDepartmentAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.AssignmentStatus == "ACTIVE"
                     && a.EffectiveFrom <= now
                     && (a.EffectiveTo == null || a.EffectiveTo > now)
                     && a.Department.IsActive
                     && a.Department.CompanyId == companyId)
            .Select(a => a.DepartmentId)
            .ToListAsync(cancellationToken);

        if (activeDepts.Count > 0)
        {
            var hasDeptGrant = await _dbContext.DepartmentPermissions
                .AsNoTracking()
                .AnyAsync(dp => activeDepts.Contains(dp.DepartmentId) && dp.PermissionCode == permissionCode, cancellationToken);
                
            if (hasDeptGrant)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<HashSet<string>> GetEffectivePermissionsInternalAsync(
        long userId,
        long? companyId,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        
        var activePermissions = await _dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => p.PermissionCode)
            .ToListAsync(cancellationToken);

        var activePermSet = new HashSet<string>(activePermissions);
        var grantedSet = new HashSet<string>();

        // 1. Department grants
        if (companyId != null)
        {
            var activeDepts = await _dbContext.UserDepartmentAssignments
                .AsNoTracking()
                .Where(a => a.UserId == userId
                         && a.AssignmentStatus == "ACTIVE"
                         && a.EffectiveFrom <= now
                         && (a.EffectiveTo == null || a.EffectiveTo > now)
                         && a.Department.IsActive
                         && a.Department.CompanyId == companyId)
                .Select(a => a.DepartmentId)
                .ToListAsync(cancellationToken);

            if (activeDepts.Count > 0)
            {
                var deptPerms = await _dbContext.DepartmentPermissions
                    .AsNoTracking()
                    .Where(dp => activeDepts.Contains(dp.DepartmentId))
                    .Select(p => p.PermissionCode)
                    .ToListAsync(cancellationToken);
                
                foreach (var p in deptPerms) grantedSet.Add(p);
            }
        }

        // 2. Role grants
        var rolePerms = await _dbContext.UserRoleAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.AssignmentStatus == "ACTIVE"
                     && a.EffectiveFrom <= now
                     && (a.EffectiveTo == null || a.EffectiveTo > now)
                     && a.Role.IsActive
                     && (a.Role.ScopeType == "GLOBAL" || a.Role.CompanyId == companyId))
            .SelectMany(a => a.Role.Permissions)
            .Select(p => p.PermissionCode)
            .ToListAsync(cancellationToken);
            
        foreach (var p in rolePerms) grantedSet.Add(p);

        // 3. Individual Allows
        var indivAllows = await _dbContext.UserIndividualPermissions
            .AsNoTracking()
            .Where(p => p.UserId == userId
                     && p.AssignmentStatus == "ACTIVE"
                     && p.GrantType == "ALLOW"
                     && p.EffectiveFrom <= now
                     && (p.EffectiveTo == null || p.EffectiveTo > now)
                     && (p.ScopeType == "GLOBAL" || p.CompanyId == companyId))
            .Select(p => p.PermissionCode)
            .ToListAsync(cancellationToken);
            
        foreach (var p in indivAllows) grantedSet.Add(p);

        // 4. Admin Group grants
        var adminPerms = await _dbContext.UserAdminGroupAssignments
            .AsNoTracking()
            .Where(a => a.UserId == userId
                     && a.AssignmentStatus == "ACTIVE"
                     && a.EffectiveFrom <= now
                     && (a.EffectiveTo == null || a.EffectiveTo > now)
                     && a.AdminGroup.IsActive
                     && (a.AdminGroup.ScopeType == "GLOBAL" || a.AdminGroup.CompanyId == companyId))
            .SelectMany(a => a.AdminGroup.Permissions)
            .Select(p => p.PermissionCode)
            .ToListAsync(cancellationToken);
            
        foreach (var p in adminPerms) grantedSet.Add(p);

        // 5. Individual Denies (These override everything)
        var indivDenies = await _dbContext.UserIndividualPermissions
            .AsNoTracking()
            .Where(p => p.UserId == userId
                     && p.AssignmentStatus == "ACTIVE"
                     && p.GrantType == "DENY"
                     && p.EffectiveFrom <= now
                     && (p.EffectiveTo == null || p.EffectiveTo > now)
                     && (p.ScopeType == "GLOBAL" || p.CompanyId == companyId))
            .Select(p => p.PermissionCode)
            .ToListAsync(cancellationToken);
            
        foreach (var p in indivDenies) grantedSet.Remove(p);

        // Finally, intersect with active permissions to filter out orphaned codes
        grantedSet.IntersectWith(activePermSet);

        return grantedSet;
    }
}
