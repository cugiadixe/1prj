using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Domain.Security.Authorization;
using Microsoft.EntityFrameworkCore;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public sealed class SecurityDepartmentPermissionsApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityDepartmentPermissionsApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<long> SetupDepartmentAsync(SecurityTestHelper helper)
    {
        var company = new PTKD.Domain.Entities.Company(
            companyCode: "COMP_" + Guid.NewGuid().ToString("N")[..8],
            parentCompanyId: null,
            name: "Test Company",
            taxCode: Guid.NewGuid().ToString("N")[..10]
        );
        helper.DbContext.Companies.Add(company);
        await helper.DbContext.SaveChangesAsync();

        var dept = new PTKD.Domain.Entities.Department(
            departmentCode: "DEPT_" + Guid.NewGuid().ToString("N")[..8],
            companyId: company.Id,
            parentDepartmentId: null,
            name: "Test Department"
        );
        helper.DbContext.Departments.Add(dept);
        await helper.DbContext.SaveChangesAsync();
        return dept.Id;
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
    public async Task SetDepartmentPermissions_UnauthorizedWithoutJwt_Returns401()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var client = _factory.CreateClient(); // No JWT
        
        var request = new SetDepartmentPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        var response = await client.PutAsJsonAsync("/api/v2/security/departments/1/permissions", request);
        
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetDepartmentPermissions_AuthenticatedWithoutAdminManage_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync(); // Default (no permissions)
        
        var request = new SetDepartmentPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        var response = await client.PutAsJsonAsync("/api/v2/security/departments/1/permissions", request);
        
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetDepartmentPermissions_WithAdminManage_ReturnsSuccessAndIncrementsPolicyVersion()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var deptId = await SetupDepartmentAsync(helper);
        var request = new SetDepartmentPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        
        var initialPolicyVersion = await helper.DbContext.AuthorizationPolicyStates.AsNoTracking().Select(p => p.PolicyVersion).SingleOrDefaultAsync() is var v and > 0 ? v : 1;

        var response = await client.PutAsJsonAsync($"/api/v2/security/departments/{deptId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        
        var newPolicyVersion = await helper.DbContext.AuthorizationPolicyStates.AsNoTracking().Select(p => p.PolicyVersion).SingleOrDefaultAsync() is var nv and > 0 ? nv : 1;
        Assert.True(newPolicyVersion > initialPolicyVersion, "Policy version should increment on mutation.");
    }

    [Fact]
    public async Task SetDepartmentPermissions_InactivePermission_Returns422()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var deptId = await SetupDepartmentAsync(helper);
        string inactiveCode = "INACTIVE_DEPT_PERM_" + Guid.NewGuid().ToString("N")[..8];
        await SetupInactivePermissionAsync(helper, inactiveCode);

        var request = new SetDepartmentPermissionsRequest(new[] { inactiveCode });
        var response = await client.PutAsJsonAsync($"/api/v2/security/departments/{deptId}/permissions", request);
        
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SetDepartmentPermissions_ExactDuplicate_Returns204Idempotent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var deptId = await SetupDepartmentAsync(helper);
        var request = new SetDepartmentPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        
        var initialResponse = await client.PutAsJsonAsync($"/api/v2/security/departments/{deptId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, initialResponse.StatusCode);

        var duplicateResponse = await client.PutAsJsonAsync($"/api/v2/security/departments/{deptId}/permissions", request);
        Assert.Equal(HttpStatusCode.NoContent, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task SetDepartmentPermissions_InvalidDepartmentId_Returns500InternalServerError()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");
        
        var request = new SetDepartmentPermissionsRequest(new[] { "SECURITY_USER_VIEW" });
        long invalidDeptId = 999999;
        
        var response = await client.PutAsJsonAsync($"/api/v2/security/departments/{invalidDeptId}/permissions", request);
        
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
