using System;
using System.Collections.Generic;

namespace PTKD.Application.PaymentManagement.DTOs;

public class CreatePaymentDraftRequest
{
    public long CustomerId { get; set; }
    public long CompanyId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePaymentItemRequest> Items { get; set; } = new();
}

public class CreatePaymentItemRequest
{
    public long ServiceId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class ConfirmPaymentRequest
{
    public string RowVersion { get; set; } = null!;
}

public class CorrectPaymentRequest
{
    public long? CustomerId { get; set; }
    public long? CompanyId { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Notes { get; set; }
    public List<CreatePaymentItemRequest>? Items { get; set; }
    public string Reason { get; set; } = null!;
    public string RowVersion { get; set; } = null!;
}

public class SoftDeletePaymentRequest
{
    public string RowVersion { get; set; } = null!;
}

public class PaymentTransactionDto
{
    public long Id { get; set; }
    public string BillCode { get; set; } = null!;
    public long CompanyId { get; set; }
    public long CustomerId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string CurrencyCode { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public long? ConfirmedByUserId { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string RowVersion { get; set; } = null!;
    public List<PaymentTransactionItemDto> Items { get; set; } = new();
}

public class PaymentTransactionItemDto
{
    public long Id { get; set; }
    public long PaymentTransactionId { get; set; }
    public long ServiceId { get; set; }
    public string ServiceTypeCode { get; set; } = null!;
    public int ServiceCycleNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentTransactionListDto
{
    public long Id { get; set; }
    public string BillCode { get; set; } = null!;
    public long CompanyId { get; set; }
    public long CustomerId { get; set; }
    public string PaymentMethod { get; set; } = null!;
    public DateTime PaymentDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
