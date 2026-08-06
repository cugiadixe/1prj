using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using PTKD.Application.Security.Authorization.DTOs;

namespace PTKD.ApiTests.Security;

/// <summary>
/// Tests for User Admin Group Assignment endpoints (OD-D-B-06, OD-D-B-07, OD-D-B-15).
/// These tests deliberately send high-precision DateTime inputs (with sub-millisecond ticks)
/// to prove that the production service normalizes to datetime2(3) before comparison and
/// persistence — not that the test itself pre-truncates the value.
/// </summary>
[Collection("Sequential")]
public sealed class SecurityAdminGroupAssignmentsApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityAdminGroupAssignmentsApiTests(SafeTestWebApplicationFactory factory)
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

    /// <summary>
    /// High-precision idempotency: sends a DateTime with sub-millisecond ticks to prove the
    /// service normalizes before comparison. First POST → 201 Created. Same second POST → 200 OK.
    /// </summary>
    [Fact]
    public async Task AssignAdminGroup_ExactDuplicate_HighPrecisionInput_Returns200Idempotent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        var groupId = await SetupAdminGroupAsync(client);

        // Build a DateTime with deliberate sub-millisecond ticks.
        var baseTime = helper.Time.GetUtcNow().UtcDateTime;
        var highPrecisionFrom = new DateTime(
            baseTime.Year, baseTime.Month, baseTime.Day,
            baseTime.Hour, baseTime.Minute, baseTime.Second,
            baseTime.Millisecond, DateTimeKind.Utc
        ).AddTicks(789_012); // sub-millisecond component

        var request = new CreateUserAdminGroupAssignmentRequest(groupId, highPrecisionFrom, null);

        // First call — must create
        var resp1 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/admin-group-assignments", request);
        var body1 = await resp1.Content.ReadAsStringAsync();
        Assert.True(resp1.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created on first assignment, got {resp1.StatusCode}. Body: {body1}");

        // Second call with the same high-precision input — must be idempotent (OD-D-B-06)
        var resp2 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/admin-group-assignments", request);
        var body2 = await resp2.Content.ReadAsStringAsync();
        Assert.True(resp2.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK idempotent on duplicate assignment, got {resp2.StatusCode}. Body: {body2}");
    }

    /// <summary>
    /// High-precision overlap: first POST creates, second POST with overlapping window returns 409.
    /// </summary>
    [Fact]
    public async Task AssignAdminGroup_TemporalOverlap_HighPrecisionInput_Returns409Conflict()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        var groupId = await SetupAdminGroupAsync(client);

        var baseTime = helper.Time.GetUtcNow().UtcDateTime;
        var highPrecisionFrom = new DateTime(
            baseTime.Year, baseTime.Month, baseTime.Day,
            baseTime.Hour, baseTime.Minute, baseTime.Second,
            baseTime.Millisecond, DateTimeKind.Utc
        ).AddTicks(345_678);

        var request1 = new CreateUserAdminGroupAssignmentRequest(groupId, highPrecisionFrom, highPrecisionFrom.AddDays(10));
        var resp1 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/admin-group-assignments", request1);
        var body1 = await resp1.Content.ReadAsStringAsync();
        Assert.True(resp1.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created on first assignment, got {resp1.StatusCode}. Body: {body1}");

        // Overlapping but non-identical window — must conflict (OD-D-B-06)
        var request2 = new CreateUserAdminGroupAssignmentRequest(groupId, highPrecisionFrom.AddDays(5), highPrecisionFrom.AddDays(15));
        var resp2 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/admin-group-assignments", request2);
        var body2 = await resp2.Content.ReadAsStringAsync();
        Assert.True(resp2.StatusCode == HttpStatusCode.Conflict,
            $"Expected 409 Conflict on overlapping assignment, got {resp2.StatusCode}. Body: {body2}");
    }
}
