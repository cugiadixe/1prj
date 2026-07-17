using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.Services;
using PTKD.Domain.Security.Authorization;
using PTKD.Domain.Entities;
using PTKD.Infrastructure.Persistence;

namespace PTKD.IntegrationTests.Security.Authorization;

[Collection("Sequential")]
public class PermissionEvaluatorIntegrationTests : IClassFixture<TestDatabaseFixture>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TestDatabaseFixture _fixture;

    public PermissionEvaluatorIntegrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var services = new ServiceCollection();
        var connectionString = fixture.ConnectionString;
        
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sql => 
            {
                sql.ExecutionStrategy(c => new PTKD.Infrastructure.Persistence.Retries.DeadlockRetryPolicy(c, 2, TimeSpan.FromMilliseconds(100)));
            });
        });
        
        services.AddScoped<IAuthorizationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
        
        // Use NullLogger for tests
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task EvaluateAsync_WithIndividualDeny_ReturnsFalse_EvenWithRoleGrant()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange
        var user = new User("EMP_TEST1", "Test User", "test1@test.com", "ACTIVE", "ACTIVE");

        var role = new Role { RoleCode = "ROLE1", Name = "Role 1", ScopeType = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };
        var permission = new Permission { PermissionCode = "PERM_TEST_1", ModuleCode = "TEST", ActionCode = "TEST", DataScope = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = permission.PermissionCode, CreatedAt = DateTime.UtcNow });
        dbContext.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = role.Id, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow });
        
        // Add Deny
        dbContext.UserIndividualPermissions.Add(new UserIndividualPermission
        {
            UserId = user.Id,
            PermissionCode = permission.PermissionCode,
            ScopeType = "GLOBAL",
            GrantType = "DENY",
            AssignmentStatus = "ACTIVE",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();

        // Act
        var result = await sut.EvaluateAsync(user.Id, "PERM_TEST_1", null);

        // Assert
        Assert.False(result, "Individual DENY should override Role Grant");
    }

    [Fact]
    public async Task EvaluateAsync_WithRoleGrant_ReturnsTrue()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange
        var user = new User("EMP_TEST2", "Test User 2", "test2@test.com", "ACTIVE", "ACTIVE");

        var role = new Role { RoleCode = "ROLE2", Name = "Role 2", ScopeType = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };
        var permission = new Permission { PermissionCode = "PERM_TEST_2", ModuleCode = "TEST", ActionCode = "TEST", DataScope = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.Roles.Add(role);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionCode = permission.PermissionCode, CreatedAt = DateTime.UtcNow });
        dbContext.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = role.Id, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.UtcNow.AddDays(-1), CreatedAt = DateTime.UtcNow });

        await dbContext.SaveChangesAsync();

        // Act
        var result = await sut.EvaluateAsync(user.Id, "PERM_TEST_2", null);

        // Assert
        Assert.True(result, "Role Grant should grant permission");
    }
}
