using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Security.Authorization;

public class CustomerCompanyScopeTests
{
    // KH1 -> cty 31; KH2 -> cty 32; KH3 -> cty 31 & 32; KH4 -> mồ côi (không công ty)
    private readonly Mock<IOrganizationDbContext> _context = new();

    public CustomerCompanyScopeTests()
    {
        _context.Setup(x => x.CustomerCompanyContexts).ReturnsDbSet(new[]
        {
            Ctx(1, 31),
            Ctx(2, 32),
            Ctx(3, 31),
            Ctx(3, 32),
        });
    }

    private static CustomerCompanyContext Ctx(long customerId, long companyId)
        => new(customerId, companyId, null, null, System.DateTime.UnixEpoch);

    private static Customer Customer(long id)
    {
        var c = new Customer($"KH{id:0000}", id);
        typeof(Customer).GetProperty("Id")!.SetValue(c, id);
        return c;
    }

    private IQueryable<Customer> FourCustomers()
        => new[] { Customer(1), Customer(2), Customer(3), Customer(4) }.AsQueryable();

    private long[] Filter(PermissionScopeResult scope)
        => CustomerCompanyScope.ApplyScope(FourCustomers(), _context.Object, scope)
            .Select(c => c.Id).OrderBy(x => x).ToArray();

    [Fact]
    public void ApplyScope_CompanyScoped_KeepsCustomersLinkedToGrantedCompany()
    {
        var scope = new PermissionScopeResult(true, false, new long[] { 31 }, System.Array.Empty<long>());
        // KH1 (31) và KH3 (31&32); KH2 (chỉ 32) và KH4 (mồ côi) không thấy.
        Assert.Equal(new long[] { 1, 3 }, Filter(scope));
    }

    [Fact]
    public void ApplyScope_Unrestricted_KeepsAllIncludingOrphan()
    {
        var scope = new PermissionScopeResult(true, true, System.Array.Empty<long>(), System.Array.Empty<long>());
        Assert.Equal(new long[] { 1, 2, 3, 4 }, Filter(scope));
    }

    [Fact]
    public void ApplyScope_UnrestrictedExcludeOneCompany_HidesCustomersOnlyInThatCompany()
    {
        var scope = new PermissionScopeResult(true, true, System.Array.Empty<long>(), new long[] { 32 });
        // KH2 chỉ ở 32 -> ẩn. KH1 (31), KH3 (còn 31), KH4 (mồ côi, toàn cục vẫn thấy).
        Assert.Equal(new long[] { 1, 3, 4 }, Filter(scope));
    }

    [Fact]
    public void ApplyScope_NotGranted_KeepsNothing()
    {
        Assert.Empty(Filter(PermissionScopeResult.Denied));
    }

    [Fact]
    public void ApplyScope_CompanyScoped_OrphanNeverVisible()
    {
        var scope = new PermissionScopeResult(true, false, new long[] { 31, 32 }, System.Array.Empty<long>());
        // KH4 mồ côi: người quyền-công-ty KHÔNG thấy dù cấp cả hai công ty.
        Assert.DoesNotContain(4L, Filter(scope));
    }
}
