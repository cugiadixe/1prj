namespace PTKD.Api.Security.Authorization;

/// <summary>
/// Stable permission code constants used across the application API.
/// </summary>
public static class PermissionCodes
{
    /// <summary>
    /// Required for all Role/AdminGroup/Assignment/DepartmentBaseline/IndividualPermission mutations
    /// and for most read operations in the security management surface.
    /// </summary>
    public const string SecurityAdminManage = "SECURITY_ADMIN_MANAGE";

    /// <summary>
    /// Read-only audit view — must NOT be used to authorize mutations.
    /// </summary>
    public const string SecurityAuditView = "SECURITY_AUDIT_VIEW";

    /// <summary>
    /// Manage Organization Users API access in Phase 1B.
    /// </summary>
    public const string OrganizationUserManage = "ORGANIZATION_USER_MANAGE";

    /// <summary>
    /// Manage Organization Departments API access in Phase 1B.
    /// </summary>
    public const string OrganizationDepartmentManage = "ORGANIZATION_DEPARTMENT_MANAGE";

    /// <summary>
    /// Manage Organization Companies API access in Phase 1B.
    /// </summary>
    public const string OrganizationCompanyManage = "ORGANIZATION_COMPANY_MANAGE";
}
