namespace PTKD.Api.Controllers.Security;

/// <summary>
/// Stable permission code constants used by D-B security controllers (OD-D-B-03).
/// </summary>
internal static class PermissionCodes
{
    /// <summary>
    /// Required for all Role/AdminGroup/Assignment/DepartmentBaseline/IndividualPermission mutations
    /// and for most read operations in the security management surface (OD-D-B-03).
    /// </summary>
    public const string SecurityAdminManage = "SECURITY_ADMIN_MANAGE";

    /// <summary>
    /// Read-only audit view — must NOT be used to authorize mutations (OD-D-B-04).
    /// Kept here as a named constant to prevent accidental reuse.
    /// </summary>
    public const string SecurityAuditView = "SECURITY_AUDIT_VIEW";
}
