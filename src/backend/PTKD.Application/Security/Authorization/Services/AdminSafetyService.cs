using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Security.Authorization.Services;

/// <inheritdoc cref="IAdminSafetyService"/>
public class AdminSafetyService : IAdminSafetyService
{
    private readonly IAuthorizationDbContext _db;

    public AdminSafetyService(IAuthorizationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> IsLastActiveHolderAsync(long userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var holders = await GetActiveHoldersAsync(permissionCode, cancellationToken);
        // Là người cuối cùng nếu chính họ đang giữ mà không còn ai KHÁC giữ.
        return holders.Contains(userId) && !holders.Any(h => h != userId);
    }

    /// <summary>
    /// Tập người dùng ĐANG HOẠT ĐỘNG được cấp <paramref name="code"/> qua bất kỳ nguồn nào
    /// (cá nhân ALLOW / vai trò / nhóm quản trị), trừ người bị CẤM cá nhân (DENY). Chốt an toàn
    /// nên thà chặt: loại vai trò/nhóm không hoạt động, loại tài khoản/việc làm không ACTIVE.
    /// </summary>
    private async Task<HashSet<long>> GetActiveHoldersAsync(string code, CancellationToken ct)
    {
        var allow = await _db.UserIndividualPermissions.AsNoTracking()
            .Where(p => p.PermissionCode == code && p.GrantType == "ALLOW" && p.AssignmentStatus == "ACTIVE")
            .Select(p => p.UserId)
            .ToListAsync(ct);

        var roleIds = await _db.RolePermissions.AsNoTracking()
            .Where(rp => rp.PermissionCode == code)
            .Join(_db.Roles.AsNoTracking().Where(r => r.IsActive), rp => rp.RoleId, r => r.Id, (rp, r) => r.Id)
            .ToListAsync(ct);
        var roleUsers = roleIds.Count == 0
            ? new List<long>()
            : await _db.UserRoleAssignments.AsNoTracking()
                .Where(a => roleIds.Contains(a.RoleId) && a.AssignmentStatus == "ACTIVE")
                .Select(a => a.UserId)
                .ToListAsync(ct);

        var groupIds = await _db.AdminGroupPermissions.AsNoTracking()
            .Where(gp => gp.PermissionCode == code)
            .Join(_db.AdminGroups.AsNoTracking().Where(g => g.IsActive), gp => gp.AdminGroupId, g => g.Id, (gp, g) => g.Id)
            .ToListAsync(ct);
        var groupUsers = groupIds.Count == 0
            ? new List<long>()
            : await _db.UserAdminGroupAssignments.AsNoTracking()
                .Where(a => groupIds.Contains(a.AdminGroupId) && a.AssignmentStatus == "ACTIVE")
                .Select(a => a.UserId)
                .ToListAsync(ct);

        var deny = await _db.UserIndividualPermissions.AsNoTracking()
            .Where(p => p.PermissionCode == code && p.GrantType == "DENY" && p.AssignmentStatus == "ACTIVE")
            .Select(p => p.UserId)
            .ToListAsync(ct);

        var granted = new HashSet<long>(allow);
        granted.UnionWith(roleUsers);
        granted.UnionWith(groupUsers);
        granted.ExceptWith(deny);
        if (granted.Count == 0)
            return granted;

        var activeIds = await _db.Users.AsNoTracking()
            .Where(u => granted.Contains(u.Id) && u.AccountStatus == "ACTIVE" && u.EmploymentStatus == "ACTIVE")
            .Select(u => u.Id)
            .ToListAsync(ct);

        return new HashSet<long>(activeIds);
    }
}
