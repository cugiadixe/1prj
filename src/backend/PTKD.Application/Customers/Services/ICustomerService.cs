using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Customers.DTOs;

namespace PTKD.Application.Customers.Services;

public interface ICustomerService
{
    Task<CustomerDetailDto> CreateCustomerAsync(CreateCustomerRequest request, long actorUserId, CancellationToken ct = default);
    Task<CustomerDetailDto> UpdateCustomerAsync(long id, UpdateCustomerRequest request, long actorUserId, CancellationToken ct = default);
    Task<CustomerDetailDto?> GetCustomerByIdAsync(long id, bool canViewSensitive, CancellationToken ct = default);
    Task<PagedResult<CustomerListItemDto>> SearchCustomersAsync(CustomerSearchRequest request, bool canViewSensitive, CancellationToken ct = default);
    Task<CompanyLookupDto[]> GetAssignedCompanyLookupsAsync(CancellationToken ct = default);
    Task<StaffLookupDto[]> GetAssignedStaffLookupsAsync(CancellationToken ct = default);
    Task<DuplicateCheckResult> CheckDuplicatesAsync(DuplicateCheckRequest request, CancellationToken ct = default);
    Task<CustomerCompanyContextDto[]> GetCompanyContextsAsync(long customerId, CancellationToken ct = default);
    Task<CustomerCompanyContextDto> CreateCompanyContextAsync(long customerId, CreateCustomerCompanyContextRequest request, long actorUserId, CancellationToken ct = default);
    Task<CustomerCompanyContextDto> UpdateCompanyContextAsync(long customerId, long contextId, UpdateCustomerCompanyContextRequest request, long actorUserId, CancellationToken ct = default);
}
