using PTKD.Application.Security.Authorization.DTOs;

namespace PTKD.Application.Security.Authorization.Interfaces;

/// <summary>
/// Facade service for all D-B security administration operations.
/// Every mutation increments Authorization_Policy_State in the same transaction (OD-D-B-05).
/// </summary>
public interface ISecurityAdminService
{
    // ── Permissions catalog (read-only) ────────────────────────────────────

    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default);
    Task<PermissionDto> GetPermissionAsync(string code, CancellationToken ct = default);

    // ── Roles ──────────────────────────────────────────────────────────────

    Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default);
    Task<RoleDto> GetRoleAsync(long id, CancellationToken ct = default);
    Task<RoleDto> CreateRoleAsync(long actorUserId, CreateRoleRequest request, CancellationToken ct = default);
    Task<RoleDto> UpdateRoleAsync(long actorUserId, long id, UpdateRoleRequest request, CancellationToken ct = default);
    Task DeactivateRoleAsync(long actorUserId, long id, DeactivateRoleRequest request, CancellationToken ct = default);
    Task AddRolePermissionsAsync(long actorUserId, long roleId, AddRolePermissionsRequest request, CancellationToken ct = default);
    Task RemoveRolePermissionAsync(long actorUserId, long roleId, string permissionCode, CancellationToken ct = default);

    // ── AdminGroups ────────────────────────────────────────────────────────

    Task<IReadOnlyList<AdminGroupDto>> ListAdminGroupsAsync(CancellationToken ct = default);
    Task<AdminGroupDto> GetAdminGroupAsync(long id, CancellationToken ct = default);
    Task<AdminGroupDto> CreateAdminGroupAsync(long actorUserId, CreateAdminGroupRequest request, CancellationToken ct = default);
    Task<AdminGroupDto> UpdateAdminGroupAsync(long actorUserId, long id, UpdateAdminGroupRequest request, CancellationToken ct = default);
    Task DeactivateAdminGroupAsync(long actorUserId, long id, DeactivateAdminGroupRequest request, CancellationToken ct = default);
    Task AddAdminGroupPermissionsAsync(long actorUserId, long groupId, AddAdminGroupPermissionsRequest request, CancellationToken ct = default);
    Task RemoveAdminGroupPermissionAsync(long actorUserId, long groupId, string permissionCode, CancellationToken ct = default);

    // ── User Role Assignments ──────────────────────────────────────────────

    Task<IReadOnlyList<UserRoleAssignmentDto>> ListUserRoleAssignmentsAsync(long userId, CancellationToken ct = default);
    /// <summary>
    /// Returns the existing assignment id on idempotent exact-duplicate (OD-D-B-06).
    /// Throws BusinessRuleValidationException with SEC_ROLE_ASSIGNMENT_CONFLICT on overlap.
    /// </summary>
    Task<(UserRoleAssignmentDto Assignment, bool WasIdempotent)> AssignRoleAsync(long actorUserId, long userId, CreateUserRoleAssignmentRequest request, CancellationToken ct = default);
    Task DeactivateUserRoleAssignmentAsync(long actorUserId, long userId, long assignmentId, DeactivateAssignmentRequest request, CancellationToken ct = default);

    // ── User AdminGroup Assignments ────────────────────────────────────────

    Task<IReadOnlyList<UserAdminGroupAssignmentDto>> ListUserAdminGroupAssignmentsAsync(long userId, CancellationToken ct = default);
    Task<(UserAdminGroupAssignmentDto Assignment, bool WasIdempotent)> AssignAdminGroupAsync(long actorUserId, long userId, CreateUserAdminGroupAssignmentRequest request, CancellationToken ct = default);
    Task DeactivateUserAdminGroupAssignmentAsync(long actorUserId, long userId, long assignmentId, DeactivateAssignmentRequest request, CancellationToken ct = default);

    // ── User Individual Permissions ────────────────────────────────────────

    Task<IReadOnlyList<UserIndividualPermissionDto>> ListUserIndividualPermissionsAsync(long userId, CancellationToken ct = default);
    Task<(UserIndividualPermissionDto Permission, bool WasIdempotent)> GrantIndividualPermissionAsync(long actorUserId, long userId, CreateUserIndividualPermissionRequest request, CancellationToken ct = default);
    Task DeactivateIndividualPermissionAsync(long actorUserId, long userId, long permissionId, DeactivateAssignmentRequest request, CancellationToken ct = default);

    // ── Department Permissions ─────────────────────────────────────────────

    Task<IReadOnlyList<DepartmentPermissionDto>> ListDepartmentPermissionsAsync(long departmentId, CancellationToken ct = default);
    Task SetDepartmentPermissionsAsync(long actorUserId, long departmentId, SetDepartmentPermissionsRequest request, CancellationToken ct = default);
    Task RemoveDepartmentPermissionAsync(long actorUserId, long departmentId, string permissionCode, CancellationToken ct = default);

    // ── Effective Permissions ──────────────────────────────────────────────

    Task<EffectivePermissionsResponse> GetEffectivePermissionsAsync(long userId, long? companyId, CancellationToken ct = default);

        Task<UserCompaniesResponse> GetSelectableCompaniesAsync(long userId, CancellationToken ct = default);
}
