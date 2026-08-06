using System;
using Xunit;
using PTKD.Domain.Entities;

namespace PTKD.UnitTests.Domain;

public class PaymentTransactionItemTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesItem()
    {
        var item = new PaymentTransactionItem(1, 10, "MONTHLY_FEE", 1, 50_000m, "test");
        Assert.Equal(1, item.PaymentTransactionId);
        Assert.Equal(10, item.ServiceId);
        Assert.Equal("MONTHLY_FEE", item.ServiceTypeCode);
        Assert.Equal(1, item.ServiceCycleNumber);
        Assert.Equal(50_000m, item.Amount);
    }

    [Fact]
    public void Constructor_ZeroAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new PaymentTransactionItem(1, 10, "FEE", 1, 0m, null));
    }

    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new PaymentTransactionItem(1, 10, "FEE", 1, -1m, null));
    }

    [Fact]
    public void Constructor_EmptyServiceTypeCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new PaymentTransactionItem(1, 10, "", 1, 50_000m, null));
    }
}
