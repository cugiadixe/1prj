using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Common.Models;
using PTKD.Application.ServiceManagement.DTOs;

namespace PTKD.Application.ServiceManagement.Services;

public interface IServiceTypeService
{
    Task<PagedResult<ServiceTypeDto>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ServiceTypeDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ServiceTypeDto> CreateAsync(CreateServiceTypeRequest request, long userId, CancellationToken ct = default);
    Task<ServiceTypeDto> UpdateAsync(long id, UpdateServiceTypeRequest request, long userId, CancellationToken ct = default);
    Task<ServiceTypeDto> DeactivateAsync(long id, string rowVersion, long userId, CancellationToken ct = default);
}
