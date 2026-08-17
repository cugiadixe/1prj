using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Cemeteries;

public interface ICemeteryService
{
    /// <summary>Danh sách nghĩa trang thuộc một công ty (để cấu hình).</summary>
    Task<IReadOnlyList<CemeteryDto>> GetByCompanyAsync(long companyId, CancellationToken ct = default);

    /// <summary>Đặt mã hoa văn chìm cho một nghĩa trang (rỗng = bỏ hoa văn).</summary>
    Task SetWatermarkAsync(long cemeteryId, string? watermarkCode, long companyId, long actorUserId, CancellationToken ct = default);
}
