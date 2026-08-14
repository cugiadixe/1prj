using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Workflows.Services;

/// <summary>
/// Kết quả phân giải người duyệt.
///
/// <paramref name="RequesterWasCandidate"/>: CHÍNH người đề xuất có nằm trong nhóm người duyệt
/// hay không (trước khi bị loại vì không được tự duyệt).
///
/// <paramref name="HadOtherCandidates"/>: có ai KHÁC người đề xuất được cấu hình làm người duyệt
/// hay không — tính TRƯỚC khi lọc tài khoản đã khoá/nghỉ việc. Cần cờ này để không nhầm ca
/// "người duyệt kia đã nghỉ việc" (lỗi cấu hình, phải chặn) thành ca "trưởng phòng tự tạo nên
/// không còn ai khác" (hợp lệ, được tự duyệt).
/// </summary>
public sealed record ApproverResolution(long[] Approvers, bool RequesterWasCandidate, bool HadOtherCandidates);

public interface IApproverResolver
{
    Task<long[]> ResolveApproversAsync(string approverSourceType, string approverSourceValue, long requesterId, long? companyId, string? processCode = null, CancellationToken ct = default);

    Task<ApproverResolution> ResolveApproversDetailedAsync(string approverSourceType, string approverSourceValue, long requesterId, long? companyId, string? processCode = null, CancellationToken ct = default);
}
