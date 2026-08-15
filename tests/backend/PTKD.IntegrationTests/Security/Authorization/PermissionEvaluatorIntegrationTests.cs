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

    // Use existing seeded permission codes from V0003 seed data.
    // These are always present after ResetToV0003() and cannot be deleted (TR_Permissions_PreventDelete).
    private const string PermGlobal = "SECURITY_ROLE_VIEW";            // GLOBAL scope
    private const string PermGlobal2 = "SECURITY_ROLE_MANAGE";         // GLOBAL scope (for multi-dept union)
    private const string PermGlobal3 = "SECURITY_PERMISSION_VIEW";     // GLOBAL scope (for inactive test — we mark a Role/AdminGroup as inactive, NOT the permission catalog)
    private const string PermGlobal4 = "SECURITY_ACCOUNT_MANAGE";      // GLOBAL scope (for effective date boundary tests)

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
        services.AddScoped<ICompanyHierarchyService, CompanyHierarchyService>();
        services.AddScoped<IPermissionEvaluator, PermissionEvaluator>();
        
        // Use NullLogger for tests
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        _serviceProvider = services.BuildServiceProvider();
    }

    // ── Existing tests ─────────────────────────────────────────────────────────

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

    // ── Correction tests — real-DB coverage ────────────────────────────────────

    /// <summary>
    /// Correction test 1: Admin Group grant overridden by individual DENY — real DB.
    /// OD-D-01: Individual DENY wins over all sources including Admin Group.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_AdminGroupGrant_OverriddenByIndividualDeny_ReturnsFalse()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange — use unique codes to avoid collision across reset-less reruns
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"EMP_AG_{suffix}", $"AgDeny {suffix}", $"ag_{suffix}@t.com", "ACTIVE", "ACTIVE");
        var adminGroup = new AdminGroup
        {
            GroupCode = $"AGDENY_{suffix}",
            Name = $"AG Deny Test {suffix}",
            ScopeType = "GLOBAL",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var permission = new Permission
        {
            PermissionCode = $"PERM_AG_{suffix}",
            ModuleCode = "TEST",
            ActionCode = "AGDENY",
            DataScope = "GLOBAL",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Users.Add(user);
        dbContext.AdminGroups.Add(adminGroup);
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync();

        // Grant via Admin Group
        dbContext.AdminGroupPermissions.Add(new AdminGroupPermission
        {
            AdminGroupId = adminGroup.Id,
            PermissionCode = permission.PermissionCode,
            CreatedAt = DateTime.UtcNow
        });
        dbContext.UserAdminGroupAssignments.Add(new UserAdminGroupAssignment
        {
            UserId = user.Id,
            AdminGroupId = adminGroup.Id,
            AssignmentStatus = "ACTIVE",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        // Individual DENY overrides the Admin Group grant
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
        var result = await sut.EvaluateAsync(user.Id, permission.PermissionCode, null);

        // Assert
        Assert.False(result, "Individual DENY must override Admin Group grant (OD-D-01)");
    }

    /// <summary>
    /// Correction test 2: Multi-department union — real DB.
    /// OD-D-02: Union of all active department baseline permissions.
    /// User belongs to two departments each with a different permission; both must be granted.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_MultiDepartmentUnion_GrantsPermissionsFromBothDepartments()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var company = new Company($"CO_{suffix}", null, $"Co {suffix}", null);
        var user = new User($"EMP_MD_{suffix}", $"MultiDept {suffix}", $"md_{suffix}@t.com", "ACTIVE", "ACTIVE");
        var dept1 = new Department($"DEPT1_{suffix}", company.Id, null, $"Dept 1 {suffix}");
        var dept2 = new Department($"DEPT2_{suffix}", company.Id, null, $"Dept 2 {suffix}");
        var perm1 = new Permission { PermissionCode = $"PERM_MD1_{suffix}", ModuleCode = "TEST", ActionCode = "MD1", DataScope = "COMPANY", IsActive = true, CreatedAt = DateTime.UtcNow };
        var perm2 = new Permission { PermissionCode = $"PERM_MD2_{suffix}", ModuleCode = "TEST", ActionCode = "MD2", DataScope = "COMPANY", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Companies.Add(company);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        // Set company_id on departments after company ID is known
        typeof(Department).GetProperty("CompanyId")!.SetValue(dept1, company.Id);
        typeof(Department).GetProperty("CompanyId")!.SetValue(dept2, company.Id);
        dbContext.Departments.Add(dept1);
        dbContext.Departments.Add(dept2);
        dbContext.Permissions.Add(perm1);
        dbContext.Permissions.Add(perm2);
        await dbContext.SaveChangesAsync();

        // Company assignment so user is scoped to the company
        var companyAssignment = new UserCompanyAssignment(user.Id, company.Id, true, DateTime.UtcNow.AddDays(-1));
        dbContext.UserCompanyAssignments.Add(companyAssignment);
        await dbContext.SaveChangesAsync();

        // Two active department assignments in the same company
        dbContext.UserDepartmentAssignments.Add(new UserDepartmentAssignment(user.Id, dept1.Id, companyAssignment.Id, company.Id, true, DateTime.UtcNow.AddDays(-1)));
        dbContext.UserDepartmentAssignments.Add(new UserDepartmentAssignment(user.Id, dept2.Id, companyAssignment.Id, company.Id, false, DateTime.UtcNow.AddDays(-1)));
        // Each department has a different permission
        dbContext.DepartmentPermissions.Add(new DepartmentPermission { DepartmentId = dept1.Id, PermissionCode = perm1.PermissionCode, CreatedAt = DateTime.UtcNow });
        dbContext.DepartmentPermissions.Add(new DepartmentPermission { DepartmentId = dept2.Id, PermissionCode = perm2.PermissionCode, CreatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        // Act — check both permissions; the union of dept1 + dept2 must grant both
        var result1 = await sut.EvaluateAsync(user.Id, perm1.PermissionCode, company.Id);
        var result2 = await sut.EvaluateAsync(user.Id, perm2.PermissionCode, company.Id);

        // Assert
        Assert.True(result1, "Dept 1 baseline permission must be granted (OD-D-02 union)");
        Assert.True(result2, "Dept 2 baseline permission must be granted (OD-D-02 union)");
    }

    /// <summary>
    /// Correction test 3: Missing company assignment returns DENY — real DB.
    /// A user requesting COMPANY-scoped permission for a company they have no dept assignment in must get DENY.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_MissingCompanyAssignment_ReturnsFalse()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var company1 = new Company($"CO1_{suffix}", null, $"Co1 {suffix}", null);
        var company2 = new Company($"CO2_{suffix}", null, $"Co2 {suffix}", null);
        var user = new User($"EMP_MC_{suffix}", $"MissCo {suffix}", $"mc_{suffix}@t.com", "ACTIVE", "ACTIVE");
        var perm = new Permission { PermissionCode = $"PERM_MC_{suffix}", ModuleCode = "TEST", ActionCode = "MC", DataScope = "COMPANY", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Companies.Add(company1);
        dbContext.Companies.Add(company2);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var dept = new Department($"DEPT_MC_{suffix}", company1.Id, null, $"Dept MC {suffix}");
        typeof(Department).GetProperty("CompanyId")!.SetValue(dept, company1.Id);
        dbContext.Departments.Add(dept);
        dbContext.Permissions.Add(perm);
        await dbContext.SaveChangesAsync();

        // User only has a dept assignment in company1
        var companyAssignment = new UserCompanyAssignment(user.Id, company1.Id, true, DateTime.UtcNow.AddDays(-1));
        dbContext.UserCompanyAssignments.Add(companyAssignment);
        await dbContext.SaveChangesAsync();

        dbContext.UserDepartmentAssignments.Add(new UserDepartmentAssignment(user.Id, dept.Id, companyAssignment.Id, company1.Id, true, DateTime.UtcNow.AddDays(-1)));
        dbContext.DepartmentPermissions.Add(new DepartmentPermission { DepartmentId = dept.Id, PermissionCode = perm.PermissionCode, CreatedAt = DateTime.UtcNow });
        await dbContext.SaveChangesAsync();

        // Act — request permission for company2 (user has no assignment there)
        var result = await sut.EvaluateAsync(user.Id, perm.PermissionCode, company2.Id);

        // Assert
        Assert.False(result, "User has no company2 dept assignment; COMPANY-scoped permission must be denied");
    }

    /// <summary>
    /// Correction test 4: Inactive permission catalog entry does not grant — real DB.
    /// OD-D-05: If is_active = 0, the permission must never be granted regardless of assignments.
    /// We insert a custom permission with is_active = 1, add an individual ALLOW, verify grant,
    /// then deactivate the permission and verify deny.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_InactivePermissionCatalogEntry_ReturnsFalse()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange — insert custom permission
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"EMP_IP_{suffix}", $"InactPerm {suffix}", $"ip_{suffix}@t.com", "ACTIVE", "ACTIVE");
        var perm = new Permission { PermissionCode = $"PERM_IP_{suffix}", ModuleCode = "TEST", ActionCode = "IP", DataScope = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.Permissions.Add(perm);
        await dbContext.SaveChangesAsync();

        dbContext.UserIndividualPermissions.Add(new UserIndividualPermission
        {
            UserId = user.Id,
            PermissionCode = perm.PermissionCode,
            ScopeType = "GLOBAL",
            GrantType = "ALLOW",
            AssignmentStatus = "ACTIVE",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Verify grant while active (baseline sanity)
        var resultActive = await sut.EvaluateAsync(user.Id, perm.PermissionCode, null);
        Assert.True(resultActive, "Permission must be granted while is_active = 1 (baseline sanity)");

        // Now deactivate the permission catalog entry (no delete trigger; UPDATE is allowed)
        await dbContext.Permissions
            .Where(p => p.PermissionCode == perm.PermissionCode)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsActive, false));

        // Clear cache by recreating the evaluator with a fresh MemoryCache scope
        using var scope2 = _serviceProvider.CreateScope();
        var freshCache = new MemoryCache(new MemoryCacheOptions());
        var dbContext2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut2 = new PermissionEvaluator(
            dbContext2,
            freshCache,
            NullLogger<PermissionEvaluator>.Instance,
            TimeProvider.System,
            new CompanyHierarchyService(dbContext2, freshCache));

        // Act — permission is now inactive
        var result = await sut2.EvaluateAsync(user.Id, perm.PermissionCode, null);

        // Assert
        Assert.False(result, "Inactive permission catalog entry must never grant (OD-D-05)");
    }

    /// <summary>
    /// Correction test 5a: Expired individual ALLOW does not grant — real DB.
    /// An assignment with effective_to in the past and status CLOSED must not grant.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_ExpiredAssignment_ReturnsFalse()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"EMP_EXP_{suffix}", $"Expired {suffix}", $"exp_{suffix}@t.com", "ACTIVE", "ACTIVE");
        var perm = new Permission { PermissionCode = $"PERM_EXP_{suffix}", ModuleCode = "TEST", ActionCode = "EXP", DataScope = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.Permissions.Add(perm);
        await dbContext.SaveChangesAsync();

        // CLOSED status required when effective_to is set (CK_UserIndividualPermissions_StatusDates)
        dbContext.UserIndividualPermissions.Add(new UserIndividualPermission
        {
            UserId = user.Id,
            PermissionCode = perm.PermissionCode,
            ScopeType = "GLOBAL",
            GrantType = "ALLOW",
            AssignmentStatus = "CLOSED",
            EffectiveFrom = DateTime.UtcNow.AddDays(-10),
            EffectiveTo = DateTime.UtcNow.AddDays(-1),    // expired yesterday
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await sut.EvaluateAsync(user.Id, perm.PermissionCode, null);

        // Assert
        Assert.False(result, "Expired assignment (effective_to in past) must not grant");
    }

    /// <summary>
    /// Correction test 5b: Future-dated individual ALLOW does not grant — real DB.
    /// An assignment with effective_from in the future and status SCHEDULED must not grant.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_FutureDatedAssignment_ReturnsFalse()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"EMP_FUT_{suffix}", $"Future {suffix}", $"fut_{suffix}@t.com", "ACTIVE", "ACTIVE");
        var perm = new Permission { PermissionCode = $"PERM_FUT_{suffix}", ModuleCode = "TEST", ActionCode = "FUT", DataScope = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.Permissions.Add(perm);
        await dbContext.SaveChangesAsync();

        // SCHEDULED status for future-dated assignments
        dbContext.UserIndividualPermissions.Add(new UserIndividualPermission
        {
            UserId = user.Id,
            PermissionCode = perm.PermissionCode,
            ScopeType = "GLOBAL",
            GrantType = "ALLOW",
            AssignmentStatus = "SCHEDULED",
            EffectiveFrom = DateTime.UtcNow.AddDays(1),   // starts tomorrow
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await sut.EvaluateAsync(user.Id, perm.PermissionCode, null);

        // Assert
        Assert.False(result, "Future-dated assignment (effective_from in future) must not grant");
    }

    /// <summary>
    /// Correction test 5c: Currently effective individual ALLOW grants — real DB.
    /// An ACTIVE assignment with effective_from in past and no effective_to must grant.
    /// </summary>
    [Fact]
    public async Task EvaluateAsync_CurrentlyEffectiveAssignment_ReturnsTrue()
    {
        _fixture.ResetToV0003();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var sut = scope.ServiceProvider.GetRequiredService<IPermissionEvaluator>();

        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = new User($"EMP_CUR_{suffix}", $"Current {suffix}", $"cur_{suffix}@t.com", "ACTIVE", "ACTIVE");
        var perm = new Permission { PermissionCode = $"PERM_CUR_{suffix}", ModuleCode = "TEST", ActionCode = "CUR", DataScope = "GLOBAL", IsActive = true, CreatedAt = DateTime.UtcNow };

        dbContext.Users.Add(user);
        dbContext.Permissions.Add(perm);
        await dbContext.SaveChangesAsync();

        // ACTIVE, effective_from in past, no effective_to → currently effective
        dbContext.UserIndividualPermissions.Add(new UserIndividualPermission
        {
            UserId = user.Id,
            PermissionCode = perm.PermissionCode,
            ScopeType = "GLOBAL",
            GrantType = "ALLOW",
            AssignmentStatus = "ACTIVE",
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            CreatedAt = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        // Act
        var result = await sut.EvaluateAsync(user.Id, perm.PermissionCode, null);

        // Assert
        Assert.True(result, "Currently effective assignment must grant");
    }
}
