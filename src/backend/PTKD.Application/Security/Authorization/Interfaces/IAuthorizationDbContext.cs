using Microsoft.EntityFrameworkCore;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authorization;

namespace PTKD.Application.Security.Authorization.Interfaces;

public interface IAuthorizationDbContext
{
    DbSet<Permission> Permissions { get; }
    DbSet<Role> Roles { get; }
    DbSet<AdminGroup> AdminGroups { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<AdminGroupPermission> AdminGroupPermissions { get; }
    DbSet<DepartmentPermission> DepartmentPermissions { get; }
    DbSet<UserRoleAssignment> UserRoleAssignments { get; }
    DbSet<UserAdminGroupAssignment> UserAdminGroupAssignments { get; }
    DbSet<UserIndividualPermission> UserIndividualPermissions { get; }
    DbSet<AuthorizationPolicyState> AuthorizationPolicyStates { get; }
    
    // Also needed to resolve active department from existing schemas
    DbSet<UserDepartmentAssignment> UserDepartmentAssignments { get; }
}
