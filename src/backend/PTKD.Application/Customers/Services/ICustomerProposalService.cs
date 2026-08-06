using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Customers.DTOs;

namespace PTKD.Application.Customers.Services;

public interface ICustomerProposalService
{
    Task<CustomerProposalDto> CreateProposalAsync(CreateCustomerProposalRequest request, long actorUserId, CancellationToken ct = default);
    Task<CustomerProposalDto?> GetProposalByIdAsync(long id, CancellationToken ct = default);
    Task<CustomerProposalDto[]> GetMyProposalsAsync(long actorUserId, CancellationToken ct = default);
}
