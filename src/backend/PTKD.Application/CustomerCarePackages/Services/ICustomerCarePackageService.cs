using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.CustomerCarePackages.DTOs;

namespace PTKD.Application.CustomerCarePackages.Services;

public interface ICustomerCarePackageService
{
    // actorUserId là BẮT BUỘC ở cả hai đường đọc: không biết ai hỏi thì không lọc được theo công
    // ty, và đó chính là lỗ đọc chéo công ty đã xác nhận (trả tên khách, mã mộ, đơn giá của công
    // ty khác; customerId là khoá tuần tự nên quét 1..N là sạch).
    Task<CustomerCarePackageDto[]> ListByCustomerAsync(long customerId, long actorUserId, CancellationToken ct = default);
    Task<CustomerCarePackageDto[]> ListByGraveAsync(long graveId, long actorUserId, CancellationToken ct = default);
    Task<CustomerCarePackageDto> CreateAsync(long? companyId, CreateCustomerCarePackageRequest request, long actorUserId, CancellationToken ct = default);
    Task<CustomerCarePackageDto> AssignGraveAsync(long id, long graveId, long actorUserId, CancellationToken ct = default);
    Task<CustomerCarePackageDto> CancelAsync(long id, long actorUserId, CancellationToken ct = default);
}
