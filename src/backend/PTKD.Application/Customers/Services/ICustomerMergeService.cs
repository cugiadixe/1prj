using System;
using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Customers.DTOs;

namespace PTKD.Application.Customers.Services;

public interface ICustomerMergeService
{
    Task<CustomerMergeRequestDto> CreateMergeRequestAsync(CreateCustomerMergeRequestDto request, long actorUserId, CancellationToken ct = default);
    Task<CustomerMergeRequestDto?> GetMergeRequestByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<CustomerMergeRequestDto>> SearchMergeRequestsAsync(int page, int pageSize, CancellationToken ct = default);
}
