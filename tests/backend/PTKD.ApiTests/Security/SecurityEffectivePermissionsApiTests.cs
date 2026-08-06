using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public sealed class SecurityEffectivePermissionsApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityEffectivePermissionsApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetEffectivePermissions_ForSelfWithoutSecurityAdminManage_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        // Granting some random permission to test self-query without ADMIN_MANAGE
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync(permissionToGrant: "SOME_RANDOM_PERM");

        var response = await client.GetAsync($"/api/v2/security/users/{userId}/effective-permissions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEffectivePermissions_ForOther_WithoutAdminManage_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync();

        var otherUserId = 99999;
        var response = await client.GetAsync($"/api/v2/security/users/{otherUserId}/effective-permissions");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetEffectivePermissions_ForOther_WithAdminManage_Returns200()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync(permissionToGrant: "SECURITY_ADMIN_MANAGE");

        var otherUserId = 99999; // even if not found, it returns empty list
        var response = await client.GetAsync($"/api/v2/security/users/{otherUserId}/effective-permissions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    [Fact]
    public async Task GetEffectivePermissions_ForSelfWithAdminManage_Returns200()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync(permissionToGrant: "SECURITY_ADMIN_MANAGE");
        
        var response = await client.GetAsync($"/api/v2/security/users/{userId}/effective-permissions");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
