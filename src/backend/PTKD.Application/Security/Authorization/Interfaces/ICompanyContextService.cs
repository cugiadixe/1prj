namespace PTKD.Application.Security.Authorization.Interfaces;

/// <summary>
/// Nguồn sự thật DUY NHẤT về "người dùng này thuộc công ty nào".
///
/// Vì sao phải có: trước đây hệ có SÁU cách khác nhau để trả lời câu hỏi này — một hàm private
/// trong WorkflowRuntimeService, header X-Company-Id do client tự khai, tham số query, một claim
/// chưa bao giờ được phát, và hai hàm private trong SecurityAdminService. Không cách nào là chuẩn,
/// nên vá được module này lại sót module kia.
/// </summary>
public interface ICompanyContextService
{
    /// <summary>Các công ty người dùng đang được phân công và còn hiệu lực.</summary>
    Task<IReadOnlyList<long>> GetMyCompanyIdsAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Người dùng có đang thuộc công ty này không. Dùng ở cổng vào để chặn việc tự khai
    /// X-Company-Id của công ty mình không thuộc.
    /// </summary>
    Task<bool> IsMemberOfAsync(long userId, long companyId, CancellationToken cancellationToken = default);
}
