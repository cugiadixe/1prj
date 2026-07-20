using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using PTKD.Application.Security.Authorization.DTOs;

namespace PTKD.ApiTests.Security;

/// <summary>
/// Tests for User Role Assignment endpoints (OD-D-B-06, OD-D-B-07, OD-D-B-15).
/// These tests deliberately send high-precision DateTime inputs (with sub-millisecond ticks)
/// to prove that the production service normalizes to datetime2(3) before comparison and
/// persistence — not that the test itself pre-truncates the value.
/// </summary>
[Collection("Sequential")]
public sealed class SecurityUserRoleAssignmentsApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityUserRoleAssignmentsApiTests(SafeTestWebApplicationFactory factory)
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

    /// <summary>
    /// High-precision idempotency: sends a DateTime with sub-millisecond ticks (567_890 ticks
    /// beyond the millisecond boundary) to prove the service normalizes before comparison.
    /// First POST → 201 Created. Identical second POST → 200 OK (idempotent, OD-D-B-06).
    /// </summary>
    [Fact]
    public async Task AssignRole_ExactDuplicate_HighPrecisionInput_Returns200Idempotent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        var roleId = await SetupRoleAsync(client);

        // Build a DateTime with deliberate sub-millisecond ticks.
        // The service must normalize this to datetime2(3) precision before comparison.
        var baseTime = helper.Time.GetUtcNow().UtcDateTime;
        var highPrecisionFrom = new DateTime(
            baseTime.Year, baseTime.Month, baseTime.Day,
            baseTime.Hour, baseTime.Minute, baseTime.Second,
            baseTime.Millisecond, DateTimeKind.Utc
        ).AddTicks(567_890); // sub-millisecond component: 0.056789 ms

        var request = new CreateUserRoleAssignmentRequest(roleId, highPrecisionFrom, null);

        // First call — must create
        var resp1 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/role-assignments", request);
        var body1 = await resp1.Content.ReadAsStringAsync();
        Assert.True(resp1.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created on first assignment, got {resp1.StatusCode}. Body: {body1}");

        // Second call with the same high-precision input — must be idempotent (OD-D-B-06)
        var resp2 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/role-assignments", request);
        var body2 = await resp2.Content.ReadAsStringAsync();
        Assert.True(resp2.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK idempotent on duplicate assignment, got {resp2.StatusCode}. Body: {body2}");
    }

    /// <summary>
    /// High-precision overlap conflict: first POST creates an assignment with sub-millisecond ticks.
    /// Second POST with a different but overlapping window returns 409 Conflict (OD-D-B-06).
    /// </summary>
    [Fact]
    public async Task AssignRole_TemporalOverlap_HighPrecisionInput_Returns409Conflict()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        var roleId = await SetupRoleAsync(client);

        var baseTime = helper.Time.GetUtcNow().UtcDateTime;
        var highPrecisionFrom = new DateTime(
            baseTime.Year, baseTime.Month, baseTime.Day,
            baseTime.Hour, baseTime.Minute, baseTime.Second,
            baseTime.Millisecond, DateTimeKind.Utc
        ).AddTicks(123_456); // sub-millisecond ticks

        var request1 = new CreateUserRoleAssignmentRequest(roleId, highPrecisionFrom, highPrecisionFrom.AddDays(10));
        var resp1 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/role-assignments", request1);
        var body1 = await resp1.Content.ReadAsStringAsync();
        Assert.True(resp1.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created on first assignment, got {resp1.StatusCode}. Body: {body1}");

        // Overlapping but non-identical window — must conflict (OD-D-B-06)
        var request2 = new CreateUserRoleAssignmentRequest(roleId, highPrecisionFrom.AddDays(5), highPrecisionFrom.AddDays(15));
        var resp2 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/role-assignments", request2);
        var body2 = await resp2.Content.ReadAsStringAsync();
        Assert.True(resp2.StatusCode == HttpStatusCode.Conflict,
            $"Expected 409 Conflict on overlapping assignment, got {resp2.StatusCode}. Body: {body2}");
    }
}
