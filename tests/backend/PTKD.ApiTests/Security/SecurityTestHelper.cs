using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;
using PTKD.Domain.Security.Authorization;
using PTKD.Infrastructure.Persistence;

namespace PTKD.ApiTests.Security;

/// <summary>
/// Helper to construct authenticated test users and seed required initial states
/// for D-B Security Management API testing (OD-D-B-03).
/// </summary>
public sealed class SecurityTestHelper : IAsyncDisposable
{
    private readonly SafeTestWebApplicationFactory _factory;
    private readonly IServiceScope _scope;
    public AppDbContext DbContext { get; }
    public IJwtAccessTokenService JwtService { get; }
    public TimeProvider Time { get; }

    public SecurityTestHelper(SafeTestWebApplicationFactory factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
        JwtService = _scope.ServiceProvider.GetRequiredService<IJwtAccessTokenService>();
        Time = _scope.ServiceProvider.GetRequiredService<TimeProvider>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_scope is IAsyncDisposable ad) await ad.DisposeAsync();
        else _scope.Dispose();
    }

    /// <summary>
    /// Creates a user, an auth account, and generates a valid JWT.
    /// Optionally seeds a specific permission via UserIndividualPermission.
    /// Returns a configured HttpClient that uses this token.
    /// </summary>
    public async Task<(HttpClient Client, long UserId, long? CompanyId)> CreateAuthenticatedClientAsync(
        string? permissionToGrant = null,
        long? companyId = null)
    {
        var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
        var now = Time.GetUtcNow().UtcDateTime;

        var user = new User($"emp_{unique}", "Test User", $"test_{unique}@ptkd.local", "ACTIVE", "ACTIVE");
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var account = UserAuthAccount.CreateInternal(user.Id, $"sub_{unique}", "hash", now);
        DbContext.UserAuthAccounts.Add(account);
        await DbContext.SaveChangesAsync();

        if (companyId.HasValue)
        {
            var companyAssignment = new UserCompanyAssignment(user.Id, companyId.Value, true, now.AddDays(-1));
            DbContext.UserCompanyAssignments.Add(companyAssignment);
            await DbContext.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(permissionToGrant))
        {
            // Seed permission into database if it doesn't exist
            var permExists = await DbContext.Permissions.AnyAsync(p => p.PermissionCode == permissionToGrant);
            if (!permExists)
            {
                DbContext.Permissions.Add(new Permission
                {
                    PermissionCode = permissionToGrant,
                    ModuleCode = "SECURITY",
                    ActionCode = "TEST",
                    DataScope = companyId.HasValue ? "COMPANY" : "GLOBAL",
                    IsActive = true,
                    CreatedAt = now
                });
                await DbContext.SaveChangesAsync();
            }

            DbContext.UserIndividualPermissions.Add(new UserIndividualPermission
            {
                UserId = user.Id,
                PermissionCode = permissionToGrant,
                ScopeType = companyId.HasValue ? "COMPANY" : "GLOBAL",
                CompanyId = companyId,
                GrantType = "ALLOW",
                AssignmentStatus = "ACTIVE",
                EffectiveFrom = now.AddDays(-1),
                CreatedAt = now,
                CreatedByUserId = user.Id
            });
            await DbContext.SaveChangesAsync();
        }

        var tokenRequest = new AccessTokenRequest(
            account.UserId,
            account.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            account.SecurityStamp,
            "admin");

        var result = JwtService.IssueAccessToken(tokenRequest);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", result.Token);

        return (client, user.Id, companyId);
    }
}
