using System;
using Xunit;
using PTKD.Domain.Entities;

namespace PTKD.UnitTests.Domain;

public class PaymentCorrectionHistoryTests
{
    [Fact]
    public void Constructor_ValidInput_CreatesHistory()
    {
        var correlationId = Guid.NewGuid();
        var history = new PaymentCorrectionHistory(1, 2, "Fix amount", "{}", "{}", "TotalAmount", correlationId, null);
        Assert.Equal(1, history.PaymentTransactionId);
        Assert.Equal("Fix amount", history.Reason);
        Assert.Equal(correlationId, history.CorrelationId);
    }

    [Fact]
    public void Constructor_EmptyReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new PaymentCorrectionHistory(1, 2, "", "{}", "{}", "Field", Guid.NewGuid(), null));
    }

    [Fact]
    public void Constructor_WhitespaceReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new PaymentCorrectionHistory(1, 2, "   ", "{}", "{}", "Field", Guid.NewGuid(), null));
    }
}
