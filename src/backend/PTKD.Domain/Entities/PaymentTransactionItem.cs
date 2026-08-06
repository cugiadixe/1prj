using System;

namespace PTKD.Domain.Entities;

public class PaymentTransactionItem
{
    public long Id { get; private set; }
    public long PaymentTransactionId { get; private set; }
    public long ServiceId { get; private set; }
    public string ServiceTypeCode { get; private set; } = null!;
    public int ServiceCycleNumber { get; private set; }
    public decimal Amount { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentTransactionItem() { }

    public PaymentTransactionItem(
        long paymentTransactionId,
        long serviceId,
        string serviceTypeCode,
        int serviceCycleNumber,
        decimal amount,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(serviceTypeCode))
            throw new ArgumentException("Service type code is required.", nameof(serviceTypeCode));
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        PaymentTransactionId = paymentTransactionId;
        ServiceId = serviceId;
        ServiceTypeCode = serviceTypeCode;
        ServiceCycleNumber = serviceCycleNumber;
        Amount = amount;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}
