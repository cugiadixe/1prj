using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Cards.Services;

/// <summary>Sinh file PDF thẻ mộ (khổ B5 gập đôi, 4 mặt) để xem trước / in.</summary>
public interface ICardDocumentService
{
    /// <summary>Render thẻ mộ theo id + công ty thành PDF (byte[]). Ném EntityNotFound nếu không tìm thấy.</summary>
    Task<byte[]> RenderCardPdfAsync(long cardId, long companyId, CancellationToken ct = default);
}
