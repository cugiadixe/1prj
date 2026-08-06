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
public sealed class SecurityRolesApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityRolesApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<long> SetupRoleAsync(HttpClient client)
    {
        var request = new CreateRoleRequest(
            RoleCode: "TEST_ROLE_" + Guid.NewGuid().ToString("N")[..8],
            Name: "Test Role",
            Description: "A role for testing",
            ScopeType: "GLOBAL",
            CompanyId: null
        );
        var response = await client.PostAsJsonAsync("/api/v2/security/roles", request);
        response.EnsureSuccessStatusCode();
        var role = await response.Content.ReadFromJsonAsync<RoleDto>();
        return role!.Id;
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
    public async Task CreateRole_Returns201_WhenValid()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        var request = new CreateRoleRequest(
            RoleCode: "TEST_ROLE_" + Guid.NewGuid().ToString("N")[..8],
            Name: "Test Role",
            Description: "A role for testing",
            ScopeType: "GLOBAL",
            CompanyId: null
        );

        var response = await client.PostAsJsonAsync("/api/v2/security/roles", request);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Expected Created, got {response.StatusCode}. Content: {content}");

        var role = await response.Content.ReadFromJsonAsync<RoleDto>();
        Assert.NotNull(role);
        Assert.Equal(request.RoleCode, role.RoleCode);
        Assert.True(role.IsActive);
    }

    [Fact]
    public async Task CreateRole_Fails_WhenCompanyScopeWithoutCompanyId()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        var request = new CreateRoleRequest(
            RoleCode: "TEST_ROLE_" + Guid.NewGuid().ToString("N")[..8],
            Name: "Test Role",
            Description: "A role for testing",
            ScopeType: "COMPANY",
            CompanyId: null // Invalid
        );

        var response = await client.PostAsJsonAsync("/api/v2/security/roles", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddRolePermissions_UnauthorizedWithoutJwt_Returns401()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var client = _factory.CreateClient(); // No JWT
        
        var request = new AddRolePermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        var response = await client.PostAsJsonAsync("/api/v2/security/roles/1/permissions", request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddRolePermissions_AuthenticatedWithoutAdminManage_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync(); // Default (no permissions)
        
        var request = new AddRolePermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        var response = await client.PostAsJsonAsync("/api/v2/security/roles/1/permissions", request);
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddRolePermissions_WithAdminManage_ReturnsSuccessAndIncrementsPolicyVersion()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var roleId = await SetupRoleAsync(client);
        var request = new AddRolePermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        
        var initialPolicyVersion = await helper.DbContext.AuthorizationPolicyStates.AsNoTracking().Select(p => p.PolicyVersion).SingleOrDefaultAsync() is var v and > 0 ? v : 1;

        var response = await client.PostAsJsonAsync($"/api/v2/security/roles/{roleId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        var newPolicyVersion = await helper.DbContext.AuthorizationPolicyStates.AsNoTracking().Select(p => p.PolicyVersion).SingleOrDefaultAsync() is var nv and > 0 ? nv : 1;
        Assert.True(newPolicyVersion > initialPolicyVersion, "Policy version should increment on mutation.");
    }

    [Fact]
    public async Task AddRolePermissions_InactivePermission_Returns422()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var roleId = await SetupRoleAsync(client);
        string inactiveCode = "INACTIVE_ROLE_PERM_" + Guid.NewGuid().ToString("N")[..8];
        await SetupInactivePermissionAsync(helper, inactiveCode);

        var request = new AddRolePermissionsRequest(new[] { inactiveCode });
        var response = await client.PostAsJsonAsync($"/api/v2/security/roles/{roleId}/permissions", request);
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AddRolePermissions_ExactDuplicate_Returns204Idempotent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var roleId = await SetupRoleAsync(client);
        var request = new AddRolePermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        
        var initialResponse = await client.PostAsJsonAsync($"/api/v2/security/roles/{roleId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, initialResponse.StatusCode);

        var duplicateResponse = await client.PostAsJsonAsync($"/api/v2/security/roles/{roleId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, duplicateResponse.StatusCode);
    }
}
