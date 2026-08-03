using System;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.ServiceManagement;

public class ServiceTypeTests
{
    [Fact]
    public void Constructor_ValidInputs_CreatesActiveServiceType()
    {
        var st = new ServiceType("TEST_CODE", "Test Service", "Description", 100_000m, 12, 1);
        Assert.Equal("TEST_CODE", st.Code);
        Assert.Equal("Test Service", st.Name);
        Assert.Equal(100_000m, st.StandardPrice);
        Assert.Equal("VND", st.StandardPriceCurrency);
        Assert.Equal(12, st.CycleDurationMonths);
        Assert.True(st.IsActive);
    }

    [Fact]
    public void Constructor_EmptyCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("", "Name", null, 100m, null, 1));
    }

    [Fact]
    public void Constructor_ZeroPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("CODE", "Name", null, 0m, null, 1));
    }

    [Fact]
    public void Constructor_NegativePrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("CODE", "Name", null, -1m, null, 1));
    }

    [Fact]
    public void Constructor_ZeroCycleDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("CODE", "Name", null, 100m, 0, 1));
    }

    [Fact]
    public void SetStandardPrice_ValidInputs_UpdatesPrice()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, 1);
        st.SetStandardPrice(200m, "Price increase", 1);
        Assert.Equal(200m, st.StandardPrice);
        Assert.NotNull(st.UpdatedAt);
    }

    [Fact]
    public void SetStandardPrice_ZeroPrice_Throws()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, 1);
        Assert.Throws<ArgumentException>(() => st.SetStandardPrice(0m, "Reason", 1));
    }

    [Fact]
    public void SetStandardPrice_EmptyReason_Throws()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, 1);
        Assert.Throws<ArgumentException>(() => st.SetStandardPrice(200m, "", 1));
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, 1);
        st.Deactivate();
        Assert.False(st.IsActive);
    }

    [Fact]
    public void Activate_SetsActive()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, 1);
        st.Deactivate();
        st.Activate();
        Assert.True(st.IsActive);
    }

    [Fact]
    public void Update_ValidInputs_UpdatesFields()
    {
        var st = new ServiceType("CODE", "Old Name", null, 100m, null, 1);
        st.Update("New Name", "New Desc", 6);
        Assert.Equal("New Name", st.Name);
        Assert.Equal("New Desc", st.Description);
        Assert.Equal(6, st.CycleDurationMonths);
    }

    [Fact]
    public void Update_EmptyName_Throws()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, 1);
        Assert.Throws<ArgumentException>(() => st.Update("", null, null));
    }
}
