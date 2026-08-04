using System;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Domain.Entities;

public class CardReprintRequestTests
{
    [Fact]
    public void Submit_ChangesStatusToPendingApproval()
    {
        var request = CardReprintRequest.CreateDraft(1, 1, 1, "REPRINT", 1, "LOST", null, 1);
        request.SetSubmitted(123);

        Assert.Equal(CardReprintRequest.StatusPendingApproval, request.Status);
        Assert.Equal(123, request.WorkflowInstanceId);
    }

    [Fact]
    public void Submit_InvalidState_Throws()
    {
        var request = CardReprintRequest.CreateDraft(1, 1, 1, "REPRINT", 1, "LOST", null, 1);
        request.SetSubmitted(123);
        request.SetApproved();

        Assert.Throws<InvalidOperationException>(() => request.SetSubmitted(456));
    }

    [Fact]
    public void Approve_ChangesStatusToApproved()
    {
        var request = CardReprintRequest.CreateDraft(1, 1, 1, "REPRINT", 1, "LOST", null, 1);
        request.SetSubmitted(123);
        request.SetApproved();

        Assert.Equal(CardReprintRequest.StatusApproved, request.Status);
    }

    [Fact]
    public void SetPaymentDraft_ChangesStatusToPendingPayment()
    {
        var request = CardReprintRequest.CreateDraft(1, 1, 1, "REPRINT", 1, "LOST", null, 1);
        request.SetSubmitted(123);
        request.SetApproved();
        request.SetPaymentDraft(999, 888, 50000m, "VND");

        Assert.Equal(CardReprintRequest.StatusPendingPayment, request.Status);
        Assert.Equal(999, request.PaymentTransactionId);
        Assert.Equal(888, request.ServiceItemId);
        Assert.Equal(50000m, request.FeeAmount);
        Assert.Equal("VND", request.FeeCurrency);
    }

    [Fact]
    public void SetPaid_ChangesStatusToPaid()
    {
        var request = CardReprintRequest.CreateDraft(1, 1, 1, "REPRINT", 1, "LOST", null, 1);
        request.SetSubmitted(123);
        request.SetApproved();
        request.SetPaymentDraft(999, 888, 50000m, "VND");
        request.SetPaid();

        Assert.Equal(CardReprintRequest.StatusPaid, request.Status);
    }

    [Fact]
    public void SetPrinted_ChangesStatusToPrinted()
    {
        var request = CardReprintRequest.CreateDraft(1, 1, 1, "REPRINT", 1, "LOST", null, 1);
        request.SetSubmitted(123);
        request.SetApproved();
        request.SetPaymentDraft(999, 888, 50000m, "VND");
        request.SetPaid();
        request.SetPrinted(2);

        Assert.Equal(CardReprintRequest.StatusPrinted, request.Status);
        Assert.Equal(2, request.PrintedByUserId);
        Assert.NotNull(request.PrintedAt);
    }

    [Fact]
    public void SetReleased_ChangesStatusToReleased()
    {
        var request = CardReprintRequest.CreateDraft(1, 1, 1, "REPRINT", 1, "LOST", null, 1);
        request.SetSubmitted(123);
        request.SetApproved();
        request.SetPaymentDraft(999, 888, 50000m, "VND");
        request.SetPaid();
        request.SetPrinted(2);
        request.SetReleased(3);

        Assert.Equal(CardReprintRequest.StatusReleased, request.Status);
        Assert.Equal(3, request.ReleasedByUserId);
        Assert.NotNull(request.ReleasedAt);
    }
}
