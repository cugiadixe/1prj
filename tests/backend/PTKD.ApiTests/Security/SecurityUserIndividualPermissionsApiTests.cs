using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using PTKD.Application.Security.Authorization.DTOs;

namespace PTKD.ApiTests.Security;

/// <summary>
/// Tests for User Individual Permission Assignment endpoints (OD-D-B-06, OD-D-B-07, OD-D-B-15).
/// These tests deliberately send high-precision DateTime inputs (with sub-millisecond ticks)
/// to prove that the production service normalizes to datetime2(3) before comparison and
/// persistence — not that the test itself pre-truncates the value.
/// </summary>
[Collection("Sequential")]
public sealed class SecurityUserIndividualPermissionsApiTests : IClassFixture<SafeTestWebApplicationFactory>
{
    private readonly SafeTestWebApplicationFactory _factory;

    public SecurityUserIndividualPermissionsApiTests(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// High-precision idempotency: sends a DateTime with sub-millisecond ticks to prove the
    /// service normalizes before comparison. First POST → 201 Created. Same second POST → 200 OK.
    /// </summary>
    [Fact]
    public async Task GrantIndividualPermission_ExactDuplicate_HighPrecisionInput_Returns200Idempotent()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        // Build a DateTime with deliberate sub-millisecond ticks.
        var baseTime = helper.Time.GetUtcNow().UtcDateTime;
        var highPrecisionFrom = new DateTime(
            baseTime.Year, baseTime.Month, baseTime.Day,
            baseTime.Hour, baseTime.Minute, baseTime.Second,
            baseTime.Millisecond, DateTimeKind.Utc
        ).AddTicks(891_234); // sub-millisecond component

        var request = new CreateUserIndividualPermissionRequest(
            PermissionCode: "SECURITY_USER_VIEW",
            ScopeType: "GLOBAL",
            CompanyId: null,
            GrantType: "ALLOW",
            EffectiveFrom: highPrecisionFrom,
            EffectiveTo: null,
            Reason: "Test grant"
        );

        // First call — must create
        var resp1 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/individual-permissions", request);
        var body1 = await resp1.Content.ReadAsStringAsync();
        Assert.True(resp1.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created on first grant, got {resp1.StatusCode}. Body: {body1}");

        // Second call with the same high-precision input — must be idempotent (OD-D-B-06)
        var resp2 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/individual-permissions", request);
        var body2 = await resp2.Content.ReadAsStringAsync();
        Assert.True(resp2.StatusCode == HttpStatusCode.OK,
            $"Expected 200 OK idempotent on duplicate grant, got {resp2.StatusCode}. Body: {body2}");
    }

    /// <summary>
    /// High-precision overlap: first POST creates, second POST with overlapping window returns 409.
    /// </summary>
    [Fact]
    public async Task GrantIndividualPermission_TemporalOverlap_HighPrecisionInput_Returns409Conflict()
    {
        await using var helper = new SecurityTestHelper(_factory);
        var (client, userId, _) = await helper.CreateAuthenticatedClientAsync("SECURITY_ADMIN_MANAGE");

        var baseTime = helper.Time.GetUtcNow().UtcDateTime;
        var highPrecisionFrom = new DateTime(
            baseTime.Year, baseTime.Month, baseTime.Day,
            baseTime.Hour, baseTime.Minute, baseTime.Second,
            baseTime.Millisecond, DateTimeKind.Utc
        ).AddTicks(456_789);

        var request1 = new CreateUserIndividualPermissionRequest(
            PermissionCode: "SECURITY_USER_VIEW",
            ScopeType: "GLOBAL",
            CompanyId: null,
            GrantType: "ALLOW",
            EffectiveFrom: highPrecisionFrom,
            EffectiveTo: highPrecisionFrom.AddDays(10),
            Reason: "Test grant"
        );
        var resp1 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/individual-permissions", request1);
        var body1 = await resp1.Content.ReadAsStringAsync();
        Assert.True(resp1.StatusCode == HttpStatusCode.Created,
            $"Expected 201 Created on first grant, got {resp1.StatusCode}. Body: {body1}");

        // Overlapping but non-identical window — must conflict (OD-D-B-06)
        var request2 = request1 with { 
            EffectiveFrom = highPrecisionFrom.AddDays(5), 
            EffectiveTo = highPrecisionFrom.AddDays(15) 
        };
        var resp2 = await client.PostAsJsonAsync($"/api/v2/security/users/{userId}/individual-permissions", request2);
        var body2 = await resp2.Content.ReadAsStringAsync();
        Assert.True(resp2.StatusCode == HttpStatusCode.Conflict,
            $"Expected 409 Conflict on overlapping grant, got {resp2.StatusCode}. Body: {body2}");
    }
}
