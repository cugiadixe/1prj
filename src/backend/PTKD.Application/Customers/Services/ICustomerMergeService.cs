using System;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Customers.DTOs;

namespace PTKD.Application.Customers.Services;

public interface ICustomerMergeService
{
    Task<CustomerMergeRequestDto> CreateMergeRequestAsync(CreateCustomerMergeRequestDto request, long actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Gửi duyệt một yêu cầu gộp đang ở DRAFT: tạo workflow instance cho quy trình
    /// CUSTOMER_MERGE_DUPLICATE và chuyển yêu cầu sang SUBMITTED. Sau khi duyệt hết các bước,
    /// engine sẽ tự gọi CustomerMergeExecutionHandler để dồn dữ liệu nguồn→đích.
    /// </summary>
    Task<CustomerMergeRequestDto> SubmitMergeRequestAsync(Guid id, long actorUserId, long? companyId, CancellationToken ct = default);

    Task<CustomerMergeRequestDto?> GetMergeRequestByIdAsync(Guid id, long actorUserId, CancellationToken ct = default);
    Task<PagedResult<CustomerMergeRequestDto>> SearchMergeRequestsAsync(int page, int pageSize, long actorUserId, CancellationToken ct = default);
}
