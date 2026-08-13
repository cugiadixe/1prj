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

    // Required for company-scope check on mutations (OD-D-B-15)
    DbSet<UserCompanyAssignment> UserCompanyAssignments { get; }

    // Nguồn dữ liệu thẩm quyền phê duyệt (APPROVAL_AUTHORITY resolver).
    DbSet<ApprovalAuthority> ApprovalAuthorities { get; }

    // Cần để lọc người duyệt đã khoá / nghỉ việc khi resolve.
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);
    Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy CreateExecutionStrategy();
    void ClearChangeTracker();
}
