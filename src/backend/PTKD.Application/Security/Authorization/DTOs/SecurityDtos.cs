namespace PTKD.Application.Security.Authorization.DTOs;

// ── Permission Catalog ────────────────────────────────────────────────────

public sealed record PermissionDto(
    string PermissionCode,
    string ModuleCode,
    string ActionCode,
    string DataScope,
    bool IsSensitive,
    bool IsDelegable,
    bool RequiresReason,
    bool IsActive,
    string? Description
);

// ── Role ──────────────────────────────────────────────────────────────────

public sealed record RoleDto(
    long Id,
    string RoleCode,
    string Name,
    string? Description,
    string ScopeType,
    long? CompanyId,
    bool IsActive,
    IReadOnlyList<string> PermissionCodes,
    string RowVersion
);

public sealed record CreateRoleRequest(
    string RoleCode,
    string Name,
    string? Description,
    string ScopeType,   // "GLOBAL" | "COMPANY"
    long? CompanyId
);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    string RowVersion
);

public sealed record DeactivateRoleRequest(
    string RowVersion
);

public sealed record AddRolePermissionsRequest(
    IReadOnlyList<string> PermissionCodes
);

// ── AdminGroup ────────────────────────────────────────────────────────────

public sealed record AdminGroupDto(
    long Id,
    string GroupCode,
    string Name,
    string? Description,
    string ScopeType,
    long? CompanyId,
    bool IsActive,
    IReadOnlyList<string> PermissionCodes,
    string RowVersion
);

public sealed record CreateAdminGroupRequest(
    string GroupCode,
    string Name,
    string? Description,
    string ScopeType,   // "GLOBAL" | "COMPANY"
    long? CompanyId
);

public sealed record UpdateAdminGroupRequest(
    string Name,
    string? Description,
    string RowVersion
);

public sealed record DeactivateAdminGroupRequest(
    string RowVersion
);

public sealed record AddAdminGroupPermissionsRequest(
    IReadOnlyList<string> PermissionCodes
);

// ── UserRoleAssignment ────────────────────────────────────────────────────

public sealed record UserRoleAssignmentDto(
    long Id,
    long UserId,
    long RoleId,
    string RoleCode,
    string RoleName,
    string AssignmentStatus,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string RowVersion
);

public sealed record CreateUserRoleAssignmentRequest(
    long RoleId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo
);

public sealed record DeactivateAssignmentRequest(
    string RowVersion
);

// ── UserAdminGroupAssignment ──────────────────────────────────────────────

public sealed record UserAdminGroupAssignmentDto(
    long Id,
    long UserId,
    long AdminGroupId,
    string GroupCode,
    string GroupName,
    string AssignmentStatus,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string RowVersion
);

public sealed record CreateUserAdminGroupAssignmentRequest(
    long AdminGroupId,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo
);

// ── UserIndividualPermission ──────────────────────────────────────────────

public sealed record UserIndividualPermissionDto(
    long Id,
    long UserId,
    string PermissionCode,
    string ScopeType,
    long? CompanyId,
    string GrantType,         // "ALLOW" | "DENY"
    string AssignmentStatus,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Reason,
    string RowVersion
);

public sealed record CreateUserIndividualPermissionRequest(
    string PermissionCode,
    string ScopeType,         // "GLOBAL" | "COMPANY"
    long? CompanyId,
    string GrantType,         // "ALLOW" | "DENY"
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string? Reason
);

// ── DepartmentPermission ──────────────────────────────────────────────────

public sealed record DepartmentPermissionDto(
    long DepartmentId,
    string PermissionCode
);

public sealed record SetDepartmentPermissionsRequest(
    IReadOnlyList<string> PermissionCodes
);

// ── EffectivePermissions ──────────────────────────────────────────────────

/// <summary>
/// Final effective permission codes only — no source breakdown (OD-D-B-11).
/// </summary>
public sealed record EffectivePermissionsResponse(
    long UserId,
    long? CompanyId,
    IReadOnlyList<string> PermissionCodes
);

// -- Current User Permissions ----------------------------------------------

/// <summary>
/// Một quyền của người đang đăng nhập, KÈM PHẠM VI THẬT được cấp.
///
/// Trước đây record này mang <c>Scope</c> = <c>data_scope</c> của DANH MỤC — đó là nhãn tĩnh của
/// mã quyền, không phải phạm vi người dùng được cấp. Hệ quả: giao diện không phân biệt được người
/// có quyền toàn cục với người chỉ có quyền một công ty, và 9 mã bị hỏi sai phạm vi nên phép kiểm
/// luôn trả false — cả nhóm menu Thanh toán cùng ~12 nút hành động chết vĩnh viễn.
/// </summary>
/// <param name="IsGlobal">Được cấp toàn cục — nhìn được dữ liệu mọi công ty.</param>
/// <param name="CompanyIds">Các công ty được cấp cụ thể (rỗng khi <paramref name="IsGlobal"/>).</param>
public sealed record CurrentUserPermissionDto(
    string PermissionCode,
    bool IsGlobal,
    IReadOnlyList<long> CompanyIds
);

public sealed record CurrentUserPermissionsResponseDto(
    IReadOnlyList<CurrentUserPermissionDto> Permissions
);
