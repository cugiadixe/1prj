using System;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Domain.Entities;

public class CustomerCarePackageTests
{
    private static CustomerCarePackage Create(int cotCount, decimal unitPrice, string pricingBasis)
        => CustomerCarePackage.Create(
            customerId: 1, serviceTypeId: 2, cotCount: cotCount, unitPrice: unitPrice,
            startDate: new DateTime(2026, 1, 1), endDate: null, notes: null,
            createdByUserId: 9, requiresApproval: false, pricingBasis: pricingBasis);

    [Fact]
    public void Create_PerCot_MultipliesByCotCount()
    {
        var pkg = Create(cotCount: 3, unitPrice: 100, ServiceType.PricingBasisPerCot);

        Assert.Equal(300, pkg.TotalPrice);
        Assert.Equal(3, pkg.CotCount);
    }

    [Fact]
    public void Create_DefaultBasis_IsPerCot()
    {
        // Không truyền pricingBasis → mặc định PER_COT (× số cốt), giữ hành vi cũ cho mọi nơi gọi khác.
        var pkg = CustomerCarePackage.Create(1, 2, cotCount: 4, unitPrice: 50,
            startDate: new DateTime(2026, 1, 1), endDate: null, notes: null, createdByUserId: 9);

        Assert.Equal(200, pkg.TotalPrice);
    }

    [Fact]
    public void Create_PerGrave_DoesNotMultiplyByCotCount()
    {
        // Sửa lỗi: gói tính theo phần mộ chỉ nhân 1, không nhân số cốt (trước đây bị tính đắt).
        var pkg = Create(cotCount: 3, unitPrice: 100, ServiceType.PricingBasisPerGrave);

        Assert.Equal(100, pkg.TotalPrice);
        Assert.Equal(3, pkg.CotCount); // vẫn lưu số cốt để khớp mộ khi gán
    }

    [Fact]
    public void Create_PerGrave_SingleCot_EqualsUnitPrice()
    {
        var pkg = Create(cotCount: 1, unitPrice: 250, ServiceType.PricingBasisPerGrave);

        Assert.Equal(250, pkg.TotalPrice);
    }
}
