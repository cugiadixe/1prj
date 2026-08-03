using System;
using System.Collections.Generic;

namespace PTKD.Domain.Entities;

public class PaymentTransaction
{
    public const string StatusDraft = "DRAFT";
    public const string StatusConfirmed = "CONFIRMED";

    public long Id { get; private set; }
    public string BillCode { get; private set; } = null!;
    public long CompanyId { get; private set; }
    public long CustomerId { get; private set; }
    public string PaymentMethod { get; private set; } = null!;
    public DateTime PaymentDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string CurrencyCode { get; private set; } = null!;
    public string Status { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public long? ConfirmedByUserId { get; private set; }
    public long CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private readonly List<PaymentTransactionItem> _items = new();
    public IReadOnlyList<PaymentTransactionItem> Items => _items.AsReadOnly();

    private PaymentTransaction() { }

    public static PaymentTransaction CreateDraft(
        string billCode,
        long companyId,
        long customerId,
        string paymentMethod,
        DateTime paymentDate,
        string? notes,
        long createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(billCode))
            throw new ArgumentException("Bill code is required.", nameof(billCode));
        if (paymentMethod != "CASH" && paymentMethod != "TRANSFER")
            throw new ArgumentException("Payment method must be CASH or TRANSFER.", nameof(paymentMethod));

        return new PaymentTransaction
        {
            BillCode = billCode,
            CompanyId = companyId,
            CustomerId = customerId,
            PaymentMethod = paymentMethod,
            PaymentDate = paymentDate,
            TotalAmount = 0,
            CurrencyCode = "VND",
            Status = StatusDraft,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void SetTotalAmount(decimal total)
    {
        if (total <= 0)
            throw new ArgumentException("Total amount must be greater than zero.", nameof(total));
        TotalAmount = total;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Confirm(long userId)
    {
        EnsureNotConfirmed();
        if (IsDeleted)
            throw new InvalidOperationException("Cannot confirm a deleted payment.");

        Status = StatusConfirmed;
        ConfirmedAt = DateTime.UtcNow;
        ConfirmedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        EnsureNotConfirmed();
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public string CorrectField(string fieldName, object? newValue)
    {
        EnsureConfirmed();

        switch (fieldName)
        {
            case nameof(Id):
            case nameof(BillCode):
            case nameof(Status):
            case nameof(CurrencyCode):
                throw new InvalidOperationException($"Cannot change {fieldName} on a confirmed payment.");
            case nameof(CompanyId):
                CompanyId = (long)newValue!;
                break;
            case nameof(CustomerId):
                CustomerId = (long)newValue!;
                break;
            case nameof(PaymentMethod):
                var method = (string)newValue!;
                if (method != "CASH" && method != "TRANSFER")
                    throw new ArgumentException("Payment method must be CASH or TRANSFER.");
                PaymentMethod = method;
                break;
            case nameof(PaymentDate):
                PaymentDate = (DateTime)newValue!;
                break;
            case nameof(Notes):
                Notes = (string?)newValue;
                break;
            default:
                throw new ArgumentException($"Unknown correctable field: {fieldName}.");
        }

        UpdatedAt = DateTime.UtcNow;
        return fieldName;
    }

    public void SetTotalAmountForCorrection(decimal total)
    {
        EnsureConfirmed();
        if (total <= 0)
            throw new ArgumentException("Total amount must be greater than zero.", nameof(total));
        TotalAmount = total;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureNotConfirmed()
    {
        if (Status == StatusConfirmed)
            throw new InvalidOperationException("Cannot modify a confirmed payment.");
    }

    private void EnsureConfirmed()
    {
        if (Status != StatusConfirmed)
            throw new InvalidOperationException("Payment must be in CONFIRMED status for this operation.");
    }
}
