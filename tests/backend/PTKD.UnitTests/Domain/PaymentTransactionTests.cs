using System;
using Xunit;
using PTKD.Domain.Entities;

namespace PTKD.UnitTests.Domain;

public class PaymentTransactionTests
{
    [Fact]
    public void CreateDraft_ValidInput_ReturnsDraftPayment()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-20260803-0001", 1, 1, "CASH", DateTime.UtcNow, "test", 1);
        Assert.Equal(PaymentTransaction.StatusDraft, entity.Status);
        Assert.Equal("VND", entity.CurrencyCode);
        Assert.False(entity.IsDeleted);
        Assert.Equal(0, entity.TotalAmount);
    }

    [Fact]
    public void CreateDraft_EmptyBillCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.CreateDraft("", 1, 1, "CASH", DateTime.UtcNow, null, 1));
    }

    [Fact]
    public void CreateDraft_InvalidPaymentMethod_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            PaymentTransaction.CreateDraft("PAY-001", 1, 1, "BITCOIN", DateTime.UtcNow, null, 1));
    }

    [Fact]
    public void SetTotalAmount_PositiveValue_SetsAmount()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(100_000m);
        Assert.Equal(100_000m, entity.TotalAmount);
    }

    [Fact]
    public void SetTotalAmount_ZeroOrNegative_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        Assert.Throws<ArgumentException>(() => entity.SetTotalAmount(0));
        Assert.Throws<ArgumentException>(() => entity.SetTotalAmount(-1));
    }

    [Fact]
    public void Confirm_DraftPayment_TransitionsToConfirmed()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(42);
        Assert.Equal(PaymentTransaction.StatusConfirmed, entity.Status);
        Assert.Equal(42, entity.ConfirmedByUserId);
        Assert.NotNull(entity.ConfirmedAt);
    }

    [Fact]
    public void Confirm_AlreadyConfirmed_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => entity.Confirm(1));
    }

    [Fact]
    public void SoftDelete_DraftPayment_SetsIsDeleted()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SoftDelete();
        Assert.True(entity.IsDeleted);
    }

    [Fact]
    public void SoftDelete_ConfirmedPayment_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => entity.SoftDelete());
    }

    [Fact]
    public void CorrectField_ConfirmedPayment_AllowsCompanyChange()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        var field = entity.CorrectField(nameof(PaymentTransaction.CompanyId), 2L);
        Assert.Equal(nameof(PaymentTransaction.CompanyId), field);
        Assert.Equal(2L, entity.CompanyId);
    }

    [Fact]
    public void CorrectField_HardInvariant_Id_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => entity.CorrectField(nameof(PaymentTransaction.Id), 99L));
    }

    [Fact]
    public void CorrectField_HardInvariant_BillCode_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => entity.CorrectField(nameof(PaymentTransaction.BillCode), "NEW"));
    }

    [Fact]
    public void CorrectField_HardInvariant_Status_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => entity.CorrectField(nameof(PaymentTransaction.Status), "DRAFT"));
    }

    [Fact]
    public void CorrectField_HardInvariant_CurrencyCode_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => entity.CorrectField(nameof(PaymentTransaction.CurrencyCode), "USD"));
    }

    [Fact]
    public void CorrectField_DraftPayment_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        Assert.Throws<InvalidOperationException>(() => entity.CorrectField(nameof(PaymentTransaction.CompanyId), 2L));
    }

    [Fact]
    public void CorrectField_PaymentMethod_ValidatesValue()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SetTotalAmount(50_000m);
        entity.Confirm(1);
        entity.CorrectField(nameof(PaymentTransaction.PaymentMethod), "TRANSFER");
        Assert.Equal("TRANSFER", entity.PaymentMethod);
        Assert.Throws<ArgumentException>(() => entity.CorrectField(nameof(PaymentTransaction.PaymentMethod), "BITCOIN"));
    }

    [Fact]
    public void Confirm_DeletedDraft_Throws()
    {
        var entity = PaymentTransaction.CreateDraft("PAY-001", 1, 1, "CASH", DateTime.UtcNow, null, 1);
        entity.SoftDelete();
        Assert.Throws<InvalidOperationException>(() => entity.Confirm(1));
    }
}
