using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using PTKD.Application.Common.Models;
using PTKD.Application.Security.Audit.DTOs;
using Xunit;

namespace PTKD.ApiTests.Security;

[Collection("Sequential")]
public sealed class SecurityAuditApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityAuditApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetAuditEvents_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v2/security/audit-events");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditEvents_WithoutPermission_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync();

        var response = await client.GetAsync("/api/v2/security/audit-events");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditEvents_WithCompanyScopedPermission_Returns403()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var company = new PTKD.Domain.Entities.Company("C01", null, "Test Company", null);
        helper.DbContext.Companies.Add(company);
        await helper.DbContext.SaveChangesAsync();
        
        // Assuming SecurityTestHelper allows passing companyId which sets permission scope to company
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_AUDIT_VIEW", company.Id);

        var response = await client.GetAsync("/api/v2/security/audit-events");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditEvents_WithGlobalPermission_Returns200AndPagedResult()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_AUDIT_VIEW");

        var response = await client.GetAsync("/api/v2/security/audit-events");
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PagedResult<SecurityAuditEventDto>>();
        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
        Assert.NotNull(result.Items);
    }

    [Fact]
    public async Task GetAuditEvents_InvalidPageSize_Returns400()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_AUDIT_VIEW");

        var response = await client.GetAsync("/api/v2/security/audit-events?pageSize=1001");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("PAGE_SIZE_EXCEEDED", problem.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public async Task GetAuditEvents_JsonResponse_DoesNotExposeRedactedFields()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, _, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_AUDIT_VIEW");

        var response = await client.GetAsync("/api/v2/security/audit-events");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        // MVP redaction: before_state_json, after_state_json, and changed_fields must never appear in the response.
        Assert.DoesNotContain("beforeStateJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("before_state_json", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("afterStateJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("after_state_json", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("changedFields", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("changed_fields", json, StringComparison.OrdinalIgnoreCase);
        // Sensitive auth fields must also never appear.
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", json, StringComparison.OrdinalIgnoreCase);
    }
}
