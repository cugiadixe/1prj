using PTKD.Application.Security.Authorization;
using PTKD.Application.Security.Authorization.Models;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Security.Authorization;

public class GraveCompanyScopeTests
{
    // Mộ id=1 (cty 31), 2 (cty 32), 3 (cty 33)
    private static Grave MakeGrave(long id, long cemeteryId, long companyId)
    {
        var grave = new Grave(cemeteryId, $"G{id}", "A", $"{id}", "SINGLE", "EMPTY",
            null, null, null, 1, null, null, null, null, null);
        typeof(Grave).GetProperty("Id")!.SetValue(grave, id);

        var cemetery = new Cemetery($"C{cemeteryId}", companyId, $"Cem {cemeteryId}", null);
        typeof(Grave).GetProperty("Cemetery")!.SetValue(grave, cemetery);
        return grave;
    }

    private static IQueryable<Grave> ThreeCompanyGraves() => new[]
    {
        MakeGrave(1, 101, 31),
        MakeGrave(2, 102, 32),
        MakeGrave(3, 103, 33),
    }.AsQueryable();

    [Fact]
    public void ApplyScope_CompanyScoped_KeepsOnlyGrantedCompanies()
    {
        var scope = new PermissionScopeResult(Granted: true, IsGlobal: false,
            CompanyIds: new long[] { 31 }, DeniedCompanyIds: System.Array.Empty<long>());

        var ids = GraveCompanyScope.ApplyScope(ThreeCompanyGraves(), scope).Select(g => g.Id).OrderBy(x => x);

        Assert.Equal(new long[] { 1 }, ids);
    }

    [Fact]
    public void ApplyScope_Unrestricted_KeepsAll()
    {
        var scope = new PermissionScopeResult(Granted: true, IsGlobal: true,
            CompanyIds: System.Array.Empty<long>(), DeniedCompanyIds: System.Array.Empty<long>());

        var ids = GraveCompanyScope.ApplyScope(ThreeCompanyGraves(), scope).Select(g => g.Id).OrderBy(x => x);

        Assert.Equal(new long[] { 1, 2, 3 }, ids);
    }

    [Fact]
    public void ApplyScope_UnrestrictedButDeniedOneCompany_ExcludesThatCompany()
    {
        var scope = new PermissionScopeResult(Granted: true, IsGlobal: true,
            CompanyIds: System.Array.Empty<long>(), DeniedCompanyIds: new long[] { 32 });

        var ids = GraveCompanyScope.ApplyScope(ThreeCompanyGraves(), scope).Select(g => g.Id).OrderBy(x => x);

        Assert.Equal(new long[] { 1, 3 }, ids); // cty 32 bị cấm dù toàn cục
    }

    [Fact]
    public void ApplyScope_NotGranted_KeepsNothing()
    {
        var ids = GraveCompanyScope.ApplyScope(ThreeCompanyGraves(), PermissionScopeResult.Denied).Select(g => g.Id);

        Assert.Empty(ids);
    }

    [Fact]
    public void ApplyScope_MultipleCompaniesGranted_KeepsAllGranted()
    {
        var scope = new PermissionScopeResult(Granted: true, IsGlobal: false,
            CompanyIds: new long[] { 31, 33 }, DeniedCompanyIds: System.Array.Empty<long>());

        var ids = GraveCompanyScope.ApplyScope(ThreeCompanyGraves(), scope).Select(g => g.Id).OrderBy(x => x);

        Assert.Equal(new long[] { 1, 3 }, ids);
    }

    [Fact]
    public void AllowsCompany_MatchesScopeAllows()
    {
        var scope = new PermissionScopeResult(Granted: true, IsGlobal: false,
            CompanyIds: new long[] { 31 }, DeniedCompanyIds: System.Array.Empty<long>());

        Assert.True(GraveCompanyScope.AllowsCompany(scope, 31));
        Assert.False(GraveCompanyScope.AllowsCompany(scope, 32));
        Assert.False(GraveCompanyScope.AllowsCompany(PermissionScopeResult.Denied, 31));
    }
}
