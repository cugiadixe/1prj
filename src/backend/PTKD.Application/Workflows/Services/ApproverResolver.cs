using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;

namespace PTKD.Application.Workflows.Services;

public class ApproverResolver : IApproverResolver
{
    private readonly IAuthorizationDbContext _authContext;
    private readonly TimeProvider _timeProvider;

    public ApproverResolver(IAuthorizationDbContext authContext, TimeProvider timeProvider)
    {
        _authContext = authContext;
        _timeProvider = timeProvider;
    }

    public async Task<long[]> ResolveApproversAsync(string approverSourceType, string approverSourceValue, long requesterId, long? companyId, string? processCode = null, CancellationToken ct = default)
    {
        var result = await ResolveApproversDetailedAsync(approverSourceType, approverSourceValue, requesterId, companyId, processCode, ct);
        return result.Approvers;
    }

    public async Task<ApproverResolution> ResolveApproversDetailedAsync(string approverSourceType, string approverSourceValue, long requesterId, long? companyId, string? processCode = null, CancellationToken ct = default)
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
            "APPROVAL_AUTHORITY" => await ResolveByApprovalAuthorityAsync(approverSourceValue, requesterId, companyId, processCode, ct),
            _ => []
        };

        // Người đề xuất CÓ nằm trong nhóm người duyệt không (trước khi bị loại vì không tự duyệt được).
        var requesterWasCandidate = userIds.Contains(requesterId);

        var candidates = userIds.Where(id => id != requesterId).Distinct().ToArray();

        // Có người khác được cấu hình làm người duyệt không — tính TRƯỚC khi lọc tài khoản bất hoạt.
        var hadOtherCandidates = candidates.Length > 0;

        if (candidates.Length == 0)
            return new ApproverResolution(candidates, requesterWasCandidate, hadOtherCandidates);

        // A6 — chỉ giữ người duyệt còn hoạt động (loại tài khoản đã khoá / nghỉ việc).
        // Áp cho MỌI loại nguồn: người đã bất hoạt không được là người duyệt hợp lệ.
        var active = await _authContext.Users
            .AsNoTracking()
            .Where(u => candidates.Contains(u.Id) && u.AccountStatus == "ACTIVE")
            .Select(u => u.Id)
            .ToArrayAsync(ct);

        return new ApproverResolution(active, requesterWasCandidate, hadOtherCandidates);
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

    /// <summary>
    /// Nguồn người duyệt từ bảng Thẩm quyền phê duyệt (Approval_Authorities).
    /// approverSourceValue = cấp thẩm quyền (mặc định 1 = Trưởng phòng).
    ///
    /// Cách tra: phòng chính đang hiệu lực của người tạo → các dòng thẩm quyền cùng công ty,
    /// cùng phòng, cùng cấp, đang trong hạn hiệu lực. Nếu có dòng uỷ quyền (delegated_from_user_id)
    /// thì CHỈ dùng dòng uỷ quyền (ngữ nghĩa THAY THẾ — D10): người được uỷ quyền thay hẳn.
    ///
    /// Khớp dòng thẩm quyền có process_code TRỐNG (áp mọi quy trình) HOẶC đúng bằng mã quy trình
    /// đang chạy. Giới hạn còn lại ở bản pilot: KHÔNG lọc theo ngưỡng tiền (chữ ký chưa mang số tiền
    /// hồ sơ) — cần bổ sung khi nối đa cấp theo tiền (D7).
    /// </summary>
    private async Task<long[]> ResolveByApprovalAuthorityAsync(string approverSourceValue, long requesterId, long? companyId, string? processCode, CancellationToken ct)
    {
        if (companyId is null)
            return []; // Thẩm quyền phê duyệt luôn gắn công ty; không có công ty thì không xác định được.

        var level = int.TryParse(approverSourceValue, out var parsed) ? parsed : 1;
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Phòng chính đang hiệu lực của người tạo, trong công ty của hồ sơ.
        var departmentId = await _authContext.UserDepartmentAssignments
            .AsNoTracking()
            .Where(uda => uda.UserId == requesterId
                && uda.AssignmentStatus == "ACTIVE"
                && uda.IsPrimaryForCompany
                && uda.CompanyId == companyId.Value)
            .Select(uda => (long?)uda.DepartmentId)
            .FirstOrDefaultAsync(ct);

        if (departmentId is null)
            return []; // Không xác định được phòng của người tạo → không đủ người duyệt (báo ở tầng submit).

        var rows = await _authContext.ApprovalAuthorities
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId.Value
                && a.DepartmentId == departmentId.Value
                && a.AuthorityLevel == level
                && a.Status == "ACTIVE"
                && (a.ProcessCode == null || a.ProcessCode == processCode)
                && a.EffectiveFrom <= now
                && (a.EffectiveTo == null || a.EffectiveTo > now))
            .Select(a => new { a.ApproverUserId, a.DelegatedFromUserId })
            .ToArrayAsync(ct);

        // Ngữ nghĩa THAY THẾ: nếu tồn tại dòng uỷ quyền thì chỉ dùng dòng uỷ quyền.
        var delegated = rows.Where(r => r.DelegatedFromUserId != null).ToArray();
        var effective = delegated.Length > 0 ? delegated : rows;

        return effective.Select(r => r.ApproverUserId).Distinct().ToArray();
    }
}
