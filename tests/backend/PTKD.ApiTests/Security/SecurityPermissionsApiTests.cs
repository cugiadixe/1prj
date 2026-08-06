using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public sealed class SecurityPermissionsApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityPermissionsApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListPermissions_WithoutAdminManage_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/security/permissions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ListPermissions_WithAdminManage_Returns200()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync(permissionToGrant: "SECURITY_ADMIN_MANAGE");

        var response = await client.GetAsync("/api/v2/security/permissions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
