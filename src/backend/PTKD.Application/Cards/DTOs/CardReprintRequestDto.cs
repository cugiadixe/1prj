using System;

namespace PTKD.Application.Cards.DTOs;

public class CardReprintRequestDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public long CardId { get; set; }
    public long RequesterId { get; set; }
    public string RequestType { get; set; } = null!;
    public int ReprintNumber { get; set; }
    public decimal? FeeAmount { get; set; }
    public string? FeeCurrency { get; set; }
    public string? ReasonCode { get; set; }
    public long? WorkflowInstanceId { get; set; }
    public long? PaymentTransactionId { get; set; }
    public long? ServiceItemId { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime? PrintedAt { get; set; }
    public long? PrintedByUserId { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public long? ReleasedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = null!;
}
