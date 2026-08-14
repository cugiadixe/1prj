using System;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.ServiceManagement;

public class ServiceTypeTests
{
    private const long CreatedByUserId = 1;

    [Fact]
    public void Constructor_ValidInputs_CreatesActiveServiceType()
    {
        var st = new ServiceType("TEST_CODE", "Test Service", "Description", 100_000m, 12, true, CreatedByUserId);
        Assert.Equal("TEST_CODE", st.Code);
        Assert.Equal("Test Service", st.Name);
        Assert.Equal(100_000m, st.StandardPrice);
        Assert.Equal("VND", st.StandardPriceCurrency);
        Assert.Equal(12, st.CycleDurationMonths);
        Assert.True(st.IsCarePackage);
        Assert.True(st.IsActive);
        Assert.Equal(CreatedByUserId, st.CreatedByUserId);
    }

    [Fact]
    public void Constructor_NotCarePackage_LeavesIsCarePackageFalse()
    {
        var st = new ServiceType("ONE_OFF", "One-off Service", null, 100m, null, false, CreatedByUserId);
        Assert.False(st.IsCarePackage);
        Assert.Null(st.CycleDurationMonths);
    }

    [Fact]
    public void Constructor_EmptyCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("", "Name", null, 100m, null, false, CreatedByUserId));
    }

    [Fact]
    public void Constructor_ZeroPrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("CODE", "Name", null, 0m, null, false, CreatedByUserId));
    }

    [Fact]
    public void Constructor_NegativePrice_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("CODE", "Name", null, -1m, null, false, CreatedByUserId));
    }

    [Fact]
    public void Constructor_ZeroCycleDuration_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ServiceType("CODE", "Name", null, 100m, 0, true, CreatedByUserId));
    }

    [Fact]
    public void SetStandardPrice_ValidInputs_UpdatesPrice()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, false, CreatedByUserId);
        st.SetStandardPrice(200m, "Price increase", CreatedByUserId);
        Assert.Equal(200m, st.StandardPrice);
        Assert.NotNull(st.UpdatedAt);
    }

    [Fact]
    public void SetStandardPrice_ZeroPrice_Throws()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, false, CreatedByUserId);
        Assert.Throws<ArgumentException>(() => st.SetStandardPrice(0m, "Reason", CreatedByUserId));
    }

    [Fact]
    public void SetStandardPrice_EmptyReason_Throws()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, false, CreatedByUserId);
        Assert.Throws<ArgumentException>(() => st.SetStandardPrice(200m, "", CreatedByUserId));
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, false, CreatedByUserId);
        st.Deactivate();
        Assert.False(st.IsActive);
    }

    [Fact]
    public void Activate_SetsActive()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, false, CreatedByUserId);
        st.Deactivate();
        st.Activate();
        Assert.True(st.IsActive);
    }

    [Fact]
    public void Update_ValidInputs_UpdatesFields()
    {
        var st = new ServiceType("CODE", "Old Name", null, 100m, null, false, CreatedByUserId);
        st.Update("New Name", "New Desc", 6, true);
        Assert.Equal("New Name", st.Name);
        Assert.Equal("New Desc", st.Description);
        Assert.Equal(6, st.CycleDurationMonths);
        Assert.True(st.IsCarePackage);
    }

    [Fact]
    public void Update_ClearsCarePackageFlag()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, 12, true, CreatedByUserId);
        st.Update("Name", null, null, false);
        Assert.False(st.IsCarePackage);
        Assert.Null(st.CycleDurationMonths);
    }

    [Fact]
    public void Update_EmptyName_Throws()
    {
        var st = new ServiceType("CODE", "Name", null, 100m, null, false, CreatedByUserId);
        Assert.Throws<ArgumentException>(() => st.Update("", null, null, false));
    }
}
