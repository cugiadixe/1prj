using System;

namespace PTKD.Domain.Entities;

/// <summary>
/// Nhật ký TỪNG LẦN in một thẻ mộ (append-only) — nguồn sự thật để đếm số lần in và phân biệt
/// in lần đầu (INITIAL) vs in lại (REPRINT). `Card.PrintCount` là bộ đếm cache cập nhật cùng
/// giao dịch với bản ghi này.
///
/// Vì sao cần: trước đây chỉ có `printed_at/printed_by` trên yêu cầu in lại (mỗi yêu cầu 1 mốc),
/// không đếm chính xác được số lần in vật lý của một thẻ, và luật "in lần 2 mới duyệt" không có
/// chỗ tựa. Bảng này append-only + UNIQUE 1 dòng INITIAL/thẻ khoá luôn lỗ hai lần in đầu song song.
/// </summary>
public class CardPrintHistory
{
    public const string TypeInitial = "INITIAL";
    public const string TypeReprint = "REPRINT";

    public long Id { get; private set; }
    public long CardId { get; private set; }
    public long CompanyId { get; private set; }
    public int PrintSequence { get; private set; }   // 1 = in lần đầu; 2,3… = in lại (cộng dồn)
    public string PrintType { get; private set; } = null!;
    public long? ReprintRequestId { get; private set; }
    public long? WorkflowInstanceId { get; private set; }
    public long PrintedByUserId { get; private set; }
    public DateTime PrintedAt { get; private set; }
    public string? ReasonCode { get; private set; }
    public string? Notes { get; private set; }

    private CardPrintHistory() { }

    public static CardPrintHistory Create(
        long cardId, long companyId, int printSequence, string printType,
        long? reprintRequestId, long? workflowInstanceId, long printedByUserId,
        string? reasonCode, string? notes)
    {
        return new CardPrintHistory
        {
            CardId = cardId,
            CompanyId = companyId,
            PrintSequence = printSequence,
            PrintType = printType,
            ReprintRequestId = reprintRequestId,
            WorkflowInstanceId = workflowInstanceId,
            PrintedByUserId = printedByUserId,
            ReasonCode = reasonCode,
            Notes = notes,
            PrintedAt = DateTime.UtcNow
        };
    }
}
