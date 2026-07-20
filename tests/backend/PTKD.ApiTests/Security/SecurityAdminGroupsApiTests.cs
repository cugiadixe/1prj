using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Domain.Security.Authorization;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public sealed class SecurityAdminGroupsApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityAdminGroupsApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<long> SetupAdminGroupAsync(HttpClient client)
    {
        var request = new CreateAdminGroupRequest(
            GroupCode: "TEST_GRP_" + Guid.NewGuid().ToString("N")[..8],
            Name: "Test Group",
            Description: "A group for testing",
            ScopeType: "GLOBAL",
            CompanyId: null
        );
        var response = await client.PostAsJsonAsync("/api/v2/security/admin-groups", request);
        response.EnsureSuccessStatusCode();
        var group = await response.Content.ReadFromJsonAsync<AdminGroupDto>();
        return group!.Id;
    }

    private async Task SetupInactivePermissionAsync(SecurityTestHelper helper, string code)
    {
        var perm = new Permission
        {
            PermissionCode = code,
            ModuleCode = "TEST",
            ActionCode = "TEST_ACTION",
            DataScope = "GLOBAL",
            IsActive = false,
            CreatedAt = helper.Time.GetUtcNow().UtcDateTime
        };
        helper.DbContext.Permissions.Add(perm);
        await helper.DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task AddAdminGroupPermissions_UnauthorizedWithoutJwt_Returns401()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var client = _factory.CreateClient(); // No JWT
        
        var request = new AddAdminGroupPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        var response = await client.PostAsJsonAsync("/api/v2/security/admin-groups/1/permissions", request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddAdminGroupPermissions_AuthenticatedWithoutAdminManage_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync(); // Default (no permissions)
        
        var request = new AddAdminGroupPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        var response = await client.PostAsJsonAsync("/api/v2/security/admin-groups/1/permissions", request);
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddAdminGroupPermissions_WithAdminManage_ReturnsSuccessAndIncrementsPolicyVersion()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var groupId = await SetupAdminGroupAsync(client);
        var request = new AddAdminGroupPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        
        var initialPolicyVersion = await helper.DbContext.AuthorizationPolicyStates.AsNoTracking().Select(p => p.PolicyVersion).SingleOrDefaultAsync() is var v and > 0 ? v : 1;

        var response = await client.PostAsJsonAsync($"/api/v2/security/admin-groups/{groupId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        var newPolicyVersion = await helper.DbContext.AuthorizationPolicyStates.AsNoTracking().Select(p => p.PolicyVersion).SingleOrDefaultAsync() is var nv and > 0 ? nv : 1;
        Assert.True(newPolicyVersion > initialPolicyVersion, "Policy version should increment on mutation.");
    }

    [Fact]
    public async Task AddAdminGroupPermissions_InactivePermission_Returns422()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var groupId = await SetupAdminGroupAsync(client);
        string inactiveCode = "INACTIVE_GRP_PERM_" + Guid.NewGuid().ToString("N")[..8];
        await SetupInactivePermissionAsync(helper, inactiveCode);

        var request = new AddAdminGroupPermissionsRequest(new[] { inactiveCode });
        var response = await client.PostAsJsonAsync($"/api/v2/security/admin-groups/{groupId}/permissions", request);
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AddAdminGroupPermissions_ExactDuplicate_Returns204Idempotent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var groupId = await SetupAdminGroupAsync(client);
        var request = new AddAdminGroupPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        
        var initialResponse = await client.PostAsJsonAsync($"/api/v2/security/admin-groups/{groupId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, initialResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync($"/api/v2/security/admin-groups/{groupId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, duplicateResponse.StatusCode);
    }
}
