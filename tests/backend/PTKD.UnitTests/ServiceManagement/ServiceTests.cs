using System;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.ServiceManagement;

public class ServiceTests
{
    [Fact]
    public void CreateStandard_ValidInputs_CreatesActiveService()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        Assert.Equal(Service.StatusActive, svc.Status);
        Assert.Equal(50_000m, svc.AppliedPrice);
        Assert.Equal(50_000m, svc.StandardPriceSnapshot);
        Assert.False(svc.IsOverridePrice);
        Assert.Equal(1, svc.CycleNumber);
        Assert.Null(svc.PreviousServiceId);
    }

    [Fact]
    public void CreateStandard_ZeroPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => Service.CreateStandard(1, 2, 3, 0m, DateTime.UtcNow, null, 10));
    }

    [Fact]
    public void CreateRenewal_ValidInputs_CreatesRenewalService()
    {
        var svc = Service.CreateRenewal(1, 2, 3, 50_000m, DateTime.UtcNow, null, 2, 100, 10);
        Assert.Equal(Service.StatusActive, svc.Status);
        Assert.Equal(2, svc.CycleNumber);
        Assert.Equal(100, svc.PreviousServiceId);
    }

    [Fact]
    public void CreateRenewal_ZeroCycleNumber_Throws()
    {
        Assert.Throws<ArgumentException>(() => Service.CreateRenewal(1, 2, 3, 50_000m, DateTime.UtcNow, null, 0, 100, 10));
    }

    [Fact]
    public void Expire_ActiveService_SetsExpired()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.Expire();
        Assert.Equal(Service.StatusExpired, svc.Status);
    }

    [Fact]
    public void Expire_ExpiredService_Throws()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.Expire();
        Assert.Throws<InvalidOperationException>(() => svc.Expire());
    }

    [Fact]
    public void Cancel_ActiveService_SetsCancelled()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.Cancel("No longer needed");
        Assert.Equal(Service.StatusCancelled, svc.Status);
    }

    [Fact]
    public void Cancel_ExpiredService_Throws()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.Expire();
        Assert.Throws<InvalidOperationException>(() => svc.Cancel("Reason"));
    }

    [Fact]
    public void SetPendingPriceOverride_ActiveService_SetsPending()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.SetPendingPriceOverride();
        Assert.Equal(Service.StatusPendingPriceOverride, svc.Status);
    }

    [Fact]
    public void SetPendingPriceOverride_ExpiredService_Throws()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.Expire();
        Assert.Throws<InvalidOperationException>(() => svc.SetPendingPriceOverride());
    }

    [Fact]
    public void ApplyPriceOverride_PendingService_AppliesOverride()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.SetPendingPriceOverride();
        svc.ApplyPriceOverride(40_000m, 999);
        Assert.Equal(Service.StatusActive, svc.Status);
        Assert.Equal(40_000m, svc.AppliedPrice);
        Assert.True(svc.IsOverridePrice);
        Assert.Equal(999, svc.OverrideApprovalRequestId);
    }

    [Fact]
    public void ApplyPriceOverride_ActiveService_Throws()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        Assert.Throws<InvalidOperationException>(() => svc.ApplyPriceOverride(40_000m, 999));
    }

    [Fact]
    public void RevertPendingOverride_PendingService_RevertsToActive()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        svc.SetPendingPriceOverride();
        svc.RevertPendingOverride();
        Assert.Equal(Service.StatusActive, svc.Status);
    }

    [Fact]
    public void RevertPendingOverride_ActiveService_Throws()
    {
        var svc = Service.CreateStandard(1, 2, 3, 50_000m, DateTime.UtcNow, null, 10);
        Assert.Throws<InvalidOperationException>(() => svc.RevertPendingOverride());
    }
}
