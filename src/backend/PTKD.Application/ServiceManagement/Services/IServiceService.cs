using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Common.Models;
using PTKD.Application.ServiceManagement.DTOs;

namespace PTKD.Application.ServiceManagement.Services;

public interface IServiceService
{
    Task<PagedResult<ServiceDto>> ListAsync(long companyId, long? customerId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<ServiceDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ServiceDto> CreateStandardAsync(CreateServiceRequest request, long userId, CancellationToken ct = default);
    Task<ServiceDto> RenewStandardAsync(long serviceId, RenewServiceRequest request, long userId, CancellationToken ct = default);
    Task<long> RequestPriceOverrideAsync(long serviceId, RequestPriceOverrideRequest request, long userId, CancellationToken ct = default);
}
