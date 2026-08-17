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
    /// Required for account lifecycle management (activate, disable, lock, unlock,
    /// admin password reset, revoke sessions). Seeded in V0003 as SECURITY_ACCOUNT_MANAGE.
    /// Added in Phase 1B.1-I per DEC-1B-I-04.
    /// </summary>
    public const string SecurityAccountManage = "SECURITY_ACCOUNT_MANAGE";

    /// <summary>
    /// Quyền RIÊNG để VÔ HIỆU / KHOÁ tài khoản người khác (disable/lock). Tách khỏi
    /// SECURITY_ACCOUNT_MANAGE để "vô hiệu người khác" là một ô cấp riêng trong ma trận — ai được
    /// cấp quyền này mới vô hiệu/khoá được người khác. Seed V0040, cấp cho mọi người đang có MANAGE.
    /// </summary>
    public const string SecurityAccountDisable = "SECURITY_ACCOUNT_DISABLE";

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

    public const string CustomerViewBasic = "CUSTOMER_VIEW_BASIC";

    public const string CustomerViewSensitive = "CUSTOMER_VIEW_SENSITIVE";

    public const string CustomerCreateFinal = "CUSTOMER_CREATE_FINAL";

    public const string CustomerMasterUpdate = "CUSTOMER_MASTER_UPDATE";

    public const string WorkflowView = "WORKFLOW_VIEW";

    /// <summary>
    /// Xem hồ sơ quy trình của MỌI công ty. Không có quyền này thì chỉ thấy hồ sơ của các công ty
    /// mình được phân công — vì nhãn đối tượng có kèm tên và mã khách hàng.
    /// </summary>
    public const string WorkflowViewAllCompanies = "WORKFLOW_VIEW_ALL_COMPANIES";
    public const string WorkflowConfigManage = "WORKFLOW_CONFIG_MANAGE";
    public const string WorkflowReject = "WORKFLOW_REJECT";
    public const string WorkflowRetryExecution = "WORKFLOW_RETRY_EXECUTION";
    public const string WorkflowPublish = "WORKFLOW_PUBLISH";
    public const string WorkflowBindProcess = "WORKFLOW_BIND_PROCESS";
    public const string WorkflowReassignPending = "WORKFLOW_REASSIGN_PENDING";
    public const string WorkflowAuditView = "WORKFLOW_AUDIT_VIEW";

    public const string CustomerChangeRequestCreate = "CUSTOMER_CHANGE_REQUEST_CREATE";

    public const string CardReprintRequestCreate = "CARD_REPRINT_REQUEST_CREATE";
    public const string CardReprintRequestView = "CARD_REPRINT_REQUEST_VIEW";

    public const string CardReprintApprove = "CARD_REPRINT_APPROVE";
    public const string CardReprintRequestReject = "CARD_REPRINT_REQUEST_REJECT";
    public const string CardReprintRequestMarkPrinted = "CARD_REPRINT_REQUEST_MARK_PRINTED";

    public const string CardIssue = "CARD_ISSUE"; // tạo/cấp thẻ mộ mới (sinh số thẻ) từ phần mộ

    public const string CarePackageView = "CARE_PACKAGE_VIEW";
    public const string CarePackageCreate = "CARE_PACKAGE_CREATE";

    public const string CarePackageApprove = "CARE_PACKAGE_APPROVE";
    public const string CarePackageReject = "CARE_PACKAGE_REJECT";
    public const string CarePackageCreatePayment = "CARE_PACKAGE_CREATE_PAYMENT";

    public const string GraveView = "GRAVE_VIEW";
    public const string GraveCreate = "GRAVE_CREATE";
    public const string GraveUpdate = "GRAVE_UPDATE";
    public const string GraveTransferOwnership = "GRAVE_TRANSFER_OWNERSHIP";
    public const string GraveAttachmentManage = "GRAVE_ATTACHMENT_MANAGE";

    public const string CustomerCarePackageView = "CUSTOMER_CARE_PACKAGE_VIEW";
    public const string CustomerCarePackageManage = "CUSTOMER_CARE_PACKAGE_MANAGE";

    public const string TagManage = "TAG_MANAGE";

    /// <summary>
    /// Khai báo thẩm quyền phê duyệt (ai được duyệt ở phòng ban/cấp nào). Tách riêng khỏi
    /// quyền sửa phòng ban vì ô này quyết định ai được duyệt tiền. Seed trong V0029. is_sensitive.
    /// </summary>
    public const string ApprovalAuthorityManage = "APPROVAL_AUTHORITY_MANAGE";
}
