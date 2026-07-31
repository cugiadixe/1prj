using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Workflows.Services;

public class ApproverResolver : IApproverResolver
{
    private readonly IAuthorizationDbContext _authContext;

    public ApproverResolver(IAuthorizationDbContext authContext)
    {
        _authContext = authContext;
    }

    public async Task<long[]> ResolveApproversAsync(string approverSourceType, string approverSourceValue, long requesterId, long? companyId, CancellationToken ct = default)
    {
        var userIds = approverSourceType switch
        {
            "SPECIFIC_USER" => long.TryParse(approverSourceValue, out var uid) ? new[] { uid } : [],
            "ROLE" => await ResolveByRoleAsync(approverSourceValue, ct),
            "ADMIN_GROUP" => await ResolveByAdminGroupAsync(approverSourceValue, ct),
            "DEPARTMENT" => long.TryParse(approverSourceValue, out var deptId)
                ? await ResolveByDepartmentAsync(deptId, ct)
                : [],
            "PERMISSION" => await ResolveByPermissionAsync(approverSourceValue, ct),
            _ => []
        };

        return userIds.Where(id => id != requesterId).Distinct().ToArray();
    }

    private async Task<long[]> ResolveByRoleAsync(string roleCode, CancellationToken ct)
    {
        return await _authContext.UserRoleAssignments
            .AsNoTracking()
            .Join(
                _authContext.Roles.Where(r => r.RoleCode == roleCode && r.IsActive),
                ura => ura.RoleId,
                r => r.Id,
                (ura, r) => ura.UserId)
            .Distinct()
            .ToArrayAsync(ct);
    }

    private async Task<long[]> ResolveByAdminGroupAsync(string groupCode, CancellationToken ct)
    {
        return await _authContext.UserAdminGroupAssignments
            .AsNoTracking()
            .Join(
                _authContext.AdminGroups.Where(g => g.GroupCode == groupCode && g.IsActive),
                uaga => uaga.AdminGroupId,
                g => g.Id,
                (uaga, g) => uaga.UserId)
            .Distinct()
            .ToArrayAsync(ct);
    }

    private async Task<long[]> ResolveByDepartmentAsync(long departmentId, CancellationToken ct)
    {
        return await _authContext.UserDepartmentAssignments
            .AsNoTracking()
            .Where(uda => uda.DepartmentId == departmentId && uda.AssignmentStatus == "ACTIVE")
            .Select(uda => uda.UserId)
            .Distinct()
            .ToArrayAsync(ct);
    }

    private async Task<long[]> ResolveByPermissionAsync(string permissionCode, CancellationToken ct)
    {
        return await _authContext.UserIndividualPermissions
            .AsNoTracking()
            .Where(uip => uip.PermissionCode == permissionCode && uip.GrantType == "ALLOW" && uip.AssignmentStatus == "ACTIVE")
            .Select(uip => uip.UserId)
            .Distinct()
            .ToArrayAsync(ct);
    }
}
