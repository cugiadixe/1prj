using System;
using Xunit;
using PTKD.Domain.Entities;

namespace PTKD.UnitTests.Domain;

public class ReconciliationPeriodTests
{
    [Fact]
    public void Create_ValidInput_CreatesOpenPeriod()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        Assert.Equal(ReconciliationPeriod.StatusOpen, period.Status);
        Assert.Equal(0, period.TotalAmount);
        Assert.Equal(0, period.TransactionCount);
    }

    [Fact]
    public void Create_InvalidPeriodType_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ReconciliationPeriod.Create(1, "WEEKLY", DateTime.UtcNow.Date));
    }

    [Fact]
    public void MarkDirty_OpenPeriod_TransitionsToDirty()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        period.MarkDirty();
        Assert.Equal(ReconciliationPeriod.StatusDirty, period.Status);
    }

    [Fact]
    public void MarkDirty_ConfirmedPeriod_Throws()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        period.Prepare(1, 100_000m, 5);
        period.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => period.MarkDirty());
    }

    [Fact]
    public void Prepare_OpenPeriod_TransitionsToPrepared()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        period.Prepare(1, 500_000m, 10);
        Assert.Equal(ReconciliationPeriod.StatusPrepared, period.Status);
        Assert.Equal(500_000m, period.TotalAmount);
        Assert.Equal(10, period.TransactionCount);
    }

    [Fact]
    public void Prepare_DirtyPeriod_TransitionsToPrepared()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        period.MarkDirty();
        period.Prepare(1, 200_000m, 3);
        Assert.Equal(ReconciliationPeriod.StatusPrepared, period.Status);
    }

    [Fact]
    public void Prepare_ConfirmedPeriod_Throws()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        period.Prepare(1, 100_000m, 1);
        period.Confirm(1);
        Assert.Throws<InvalidOperationException>(() => period.Prepare(1, 200_000m, 2));
    }

    [Fact]
    public void Confirm_PreparedPeriod_TransitionsToConfirmed()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        period.Prepare(1, 100_000m, 1);
        period.Confirm(2);
        Assert.Equal(ReconciliationPeriod.StatusConfirmed, period.Status);
        Assert.Equal(2, period.ConfirmedByUserId);
        Assert.NotNull(period.ConfirmedAt);
    }

    [Fact]
    public void Confirm_OpenPeriod_Throws()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        Assert.Throws<InvalidOperationException>(() => period.Confirm(1));
    }

    [Fact]
    public void Confirm_DirtyPeriod_Throws()
    {
        var period = ReconciliationPeriod.Create(1, "DAILY", DateTime.UtcNow.Date);
        period.MarkDirty();
        Assert.Throws<InvalidOperationException>(() => period.Confirm(1));
    }
}
