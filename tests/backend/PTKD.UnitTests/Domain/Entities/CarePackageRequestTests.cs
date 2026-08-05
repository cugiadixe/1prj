using System;
using System.Linq;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Domain.Entities;

public class CarePackageRequestTests
{
    [Fact]
    public void CreateDraft_InitializesCorrectly()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, 3, new DateTime(2025, 1, 1), 4);
        
        Assert.Equal(1, request.CompanyId);
        Assert.Equal(2, request.CustomerId);
        Assert.Equal(3, request.ServiceId);
        Assert.Equal(new DateTime(2025, 1, 1), request.SaleDate);
        Assert.Equal(CarePackageRequest.StatusDraft, request.Status);
        Assert.Equal(4, request.CreatedByUserId);
        Assert.Empty(request.Items);
    }

    [Fact]
    public void AddItem_UpdatesSubtotal_And_Totals()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        
        var item = CarePackageRequestItem.Create(
            "G1", 
            cotCountSnapshot: 2, 
            servicePeriodStartDate: new DateTime(2025, 1, 1), 
            servicePeriodEndDate: new DateTime(2025, 1, 1).AddYears(1).AddDays(-1), 
            unitPriceSnapshot: 100);

        request.AddItem(item);

        Assert.Single(request.Items);
        Assert.Equal(200, request.SubtotalAmount);
        Assert.Equal(200, request.TotalAmount);
    }

    [Fact]
    public void SetDiscount_ValidDiscount_UpdatesTotalsAndApprovalRequirement()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        var item = CarePackageRequestItem.Create(
            "G1", 2, new DateTime(2025, 1, 1), new DateTime(2026, 1, 1).AddDays(-1), 100);
        
        request.AddItem(item); // Subtotal = 200

        request.SetDiscount(50, "Manager approved");
        request.EvaluateApprovalRequirement();

        Assert.Equal(50, request.DiscountAmount);
        Assert.Equal("Manager approved", request.DiscountReason);
        Assert.Equal(150, request.TotalAmount);
        Assert.True(request.RequiresApproval);
    }

    [Fact]
    public void SetDiscount_NegativeAmount_Throws()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        
        Assert.Throws<InvalidOperationException>(() => request.SetDiscount(-10, "Reason"));
    }

    [Fact]
    public void SetDiscount_ExceedsSubtotal_Throws()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        var item = CarePackageRequestItem.Create(
            "G1", 1, new DateTime(2025, 1, 1), new DateTime(2026, 1, 1).AddDays(-1), 100);
        
        request.AddItem(item); // Subtotal = 100

        Assert.Throws<InvalidOperationException>(() => request.SetDiscount(150, "Reason"));
    }

    [Fact]
    public void SetDiscount_MissingReason_Throws()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        var item = CarePackageRequestItem.Create(
            "G1", 1, new DateTime(2025, 1, 1), new DateTime(2026, 1, 1).AddDays(-1), 100);
        
        request.AddItem(item);

        Assert.Throws<InvalidOperationException>(() => request.SetDiscount(50, ""));
    }

    [Fact]
    public void CarePackageRequestItem_Create_InvalidPeriod_Throws()
    {
        var start = new DateTime(2025, 1, 1);
        var end = new DateTime(2025, 6, 1); // Not 1 year

        Assert.Throws<ArgumentException>(() => CarePackageRequestItem.Create("G1", 1, start, end, 100));
    }

    [Fact]
    public void SetPaymentEligible_WithRequiresApproval_Throws()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        var item = CarePackageRequestItem.Create(
            "G1", 1, new DateTime(2025, 1, 1), new DateTime(2026, 1, 1).AddDays(-1), 100);
        request.AddItem(item);
        request.SetDiscount(10, "Reason");
        request.EvaluateApprovalRequirement(); // RequiresApproval = true

        Assert.Throws<InvalidOperationException>(() => request.SetPaymentEligible());
    }

    [Fact]
    public void SetPaymentEligible_WithoutRequiresApproval_Succeeds()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        var item = CarePackageRequestItem.Create(
            "G1", 1, new DateTime(2025, 1, 1), new DateTime(2026, 1, 1).AddDays(-1), 100);
        request.AddItem(item);
        request.EvaluateApprovalRequirement(); // RequiresApproval = false

        request.SetPaymentEligible();

        Assert.Equal(CarePackageRequest.StatusPaymentEligible, request.Status);
    }

    [Fact]
    public void B2Transitions_ApprovalRequired_Succeeds()
    {
        var request = CarePackageRequest.CreateDraft(1, 2, null, DateTime.UtcNow, 4);
        var item = CarePackageRequestItem.Create(
            "G1", 1, new DateTime(2025, 1, 1), new DateTime(2026, 1, 1).AddDays(-1), 100);
        request.AddItem(item);
        request.SetDiscount(10, "Reason");
        request.EvaluateApprovalRequirement(); // RequiresApproval = true

        request.SetSubmitted(1);
        Assert.Equal(CarePackageRequest.StatusPendingApproval, request.Status);

        request.SetApproved();
        Assert.Equal(CarePackageRequest.StatusApproved, request.Status);

        request.SetPaymentEligible();
        Assert.Equal(CarePackageRequest.StatusPaymentEligible, request.Status);

        request.SetPaymentDraft(123);
        Assert.Equal(CarePackageRequest.StatusPendingPayment, request.Status);

        request.SetPaid();
        Assert.Equal(CarePackageRequest.StatusPaid, request.Status);

        request.SetActive();
        Assert.Equal(CarePackageRequest.StatusActive, request.Status);
    }
}
