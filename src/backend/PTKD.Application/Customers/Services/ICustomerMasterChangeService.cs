using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Customers.DTOs;

namespace PTKD.Application.Customers.Services;

public interface ICustomerMasterChangeService
{
    Task<CustomerMasterChangeDto> CreateChangeRequestAsync(CreateCustomerMasterChangeRequest request, long actorUserId, long? companyId = null, CancellationToken ct = default);
    Task<CustomerMasterChangeDto?> GetChangeRequestByIdAsync(long id, CancellationToken ct = default);
    Task<CustomerMasterChangeDto[]> GetMyChangeRequestsAsync(long actorUserId, CancellationToken ct = default);
}
