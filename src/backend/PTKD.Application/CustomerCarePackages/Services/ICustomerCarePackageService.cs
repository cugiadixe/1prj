using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.CustomerCarePackages.DTOs;

namespace PTKD.Application.CustomerCarePackages.Services;

public interface ICustomerCarePackageService
{
    Task<CustomerCarePackageDto[]> ListByCustomerAsync(long customerId, CancellationToken ct = default);
    Task<CustomerCarePackageDto[]> ListByGraveAsync(long graveId, CancellationToken ct = default);
    Task<CustomerCarePackageDto> CreateAsync(long? companyId, CreateCustomerCarePackageRequest request, long actorUserId, CancellationToken ct = default);
    Task<CustomerCarePackageDto> AssignGraveAsync(long id, long graveId, long actorUserId, CancellationToken ct = default);
    Task<CustomerCarePackageDto> CancelAsync(long id, long actorUserId, CancellationToken ct = default);
}
