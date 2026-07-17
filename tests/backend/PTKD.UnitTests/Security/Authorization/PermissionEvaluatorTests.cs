using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.EntityFrameworkCore;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Application.Security.Authorization.Services;
using PTKD.Domain.Security.Authorization;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Security.Authorization;

public class PermissionEvaluatorTests
{
    private readonly Mock<IAuthorizationDbContext> _dbContextMock;
    private readonly IMemoryCache _cache;
    private readonly PermissionEvaluator _sut;
    private readonly TimeProvider _timeProvider;

    public PermissionEvaluatorTests()
    {
        _dbContextMock = new Mock<IAuthorizationDbContext>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _timeProvider = TimeProvider.System;
        _sut = new PermissionEvaluator(
            _dbContextMock.Object, 
            _cache, 
            NullLogger<PermissionEvaluator>.Instance, 
            _timeProvider);

        _dbContextMock.Setup(x => x.AuthorizationPolicyStates)
            .ReturnsDbSet(new List<AuthorizationPolicyState>());
    }

    private void SetupPermissions(IEnumerable<Permission> permissions)
    {
        _dbContextMock.Setup(x => x.Permissions).ReturnsDbSet(permissions);
    }
    private void SetupRoleAssignments(IEnumerable<UserRoleAssignment> assignments)
    {
        _dbContextMock.Setup(x => x.UserRoleAssignments).ReturnsDbSet(assignments);
    }
    private void SetupDepartmentAssignments(IEnumerable<UserDepartmentAssignment> assignments)
    {
        _dbContextMock.Setup(x => x.UserDepartmentAssignments).ReturnsDbSet(assignments);
    }
    private void SetupDepartmentPermissions(IEnumerable<DepartmentPermission> deptPerms)
    {
        _dbContextMock.Setup(x => x.DepartmentPermissions).ReturnsDbSet(deptPerms);
    }
    private void SetupIndividualPermissions(IEnumerable<UserIndividualPermission> indivPerms)
    {
        _dbContextMock.Setup(x => x.UserIndividualPermissions).ReturnsDbSet(indivPerms);
    }
    private void SetupAdminGroupAssignments(IEnumerable<UserAdminGroupAssignment> adminPerms)
    {
        _dbContextMock.Setup(x => x.UserAdminGroupAssignments).ReturnsDbSet(adminPerms);
    }

    private UserDepartmentAssignment CreateAssignment(long userId, long departmentId, long companyId, DateTime effectiveFrom, string status = "ACTIVE")
    {
        var dept = new Department("TEST", companyId, null, "Test Dept");
        typeof(Department).GetProperty("Id")!.SetValue(dept, departmentId);
        typeof(Department).GetProperty("IsActive")!.SetValue(dept, true);

        var assignment = new UserDepartmentAssignment(userId, departmentId, 1, companyId, true, effectiveFrom);
        typeof(UserDepartmentAssignment).GetProperty("AssignmentStatus")!.SetValue(assignment, status);
        typeof(UserDepartmentAssignment).GetProperty("Department")!.SetValue(assignment, dept);
        
        return assignment;
    }

    [Fact]
    public async Task Evaluate_DepartmentBaseline_GrantsPermission()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "COMPANY" } });
        SetupIndividualPermissions(new List<UserIndividualPermission>());
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        
        SetupDepartmentAssignments(new[] { CreateAssignment(1, 10, 100, DateTime.MinValue) });
        SetupDepartmentPermissions(new[] {
            new DepartmentPermission { DepartmentId = 10, PermissionCode = "PERM1" }
        });

        var result = await _sut.EvaluateAsync(1, "PERM1", 100);
        Assert.True(result);
    }

    [Fact]
    public async Task Evaluate_RoleGrant_GrantsPermission()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new List<UserIndividualPermission>());
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        SetupRoleAssignments(new[] {
            new UserRoleAssignment { 
                UserId = 1, 
                AssignmentStatus = "ACTIVE", 
                EffectiveFrom = DateTime.MinValue, 
                Role = new Role { 
                    Id = 20, 
                    IsActive = true, 
                    ScopeType = "GLOBAL",
                    Permissions = new List<RolePermission> { new RolePermission { PermissionCode = "PERM1" } }
                } 
            }
        });

        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.True(result);
    }

    [Fact]
    public async Task Evaluate_AdminGroupGrant_GrantsPermission()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new List<UserIndividualPermission>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        SetupAdminGroupAssignments(new[] {
            new UserAdminGroupAssignment { 
                UserId = 1, 
                AssignmentStatus = "ACTIVE", 
                EffectiveFrom = DateTime.MinValue, 
                AdminGroup = new AdminGroup { 
                    Id = 30, 
                    IsActive = true, 
                    ScopeType = "GLOBAL",
                    Permissions = new List<AdminGroupPermission> { new AdminGroupPermission { PermissionCode = "PERM1" } }
                } 
            }
        });

        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.True(result);
    }

    [Fact]
    public async Task Evaluate_IndividualAllow_GrantsPermission()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "COMPANY" } });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "COMPANY", CompanyId = 100 }
        });

        var result = await _sut.EvaluateAsync(1, "PERM1", 100);
        Assert.True(result);
    }

    [Fact]
    public async Task Evaluate_IndividualDeny_OverridesDepartmentGrant()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "COMPANY" } });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        
        SetupDepartmentAssignments(new[] { CreateAssignment(1, 10, 100, DateTime.MinValue) });
        SetupDepartmentPermissions(new[] {
            new DepartmentPermission { DepartmentId = 10, PermissionCode = "PERM1" }
        });

        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "DENY", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "COMPANY", CompanyId = 100 }
        });

        var result = await _sut.EvaluateAsync(1, "PERM1", 100);
        Assert.False(result); // Deny wins
    }

    [Fact]
    public async Task Evaluate_IndividualDeny_OverridesRoleGrant()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        SetupRoleAssignments(new[] {
            new UserRoleAssignment { 
                UserId = 1, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, 
                Role = new Role { Id = 20, IsActive = true, ScopeType = "GLOBAL", Permissions = new List<RolePermission> { new RolePermission { PermissionCode = "PERM1" } } } 
            }
        });

        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "DENY", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" }
        });

        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result); // Deny wins
    }

    [Fact]
    public async Task Evaluate_IndividualDeny_OverridesAdminGroupGrant()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        
        SetupAdminGroupAssignments(new[] {
            new UserAdminGroupAssignment { 
                UserId = 1, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, 
                AdminGroup = new AdminGroup { Id = 30, IsActive = true, ScopeType = "GLOBAL", Permissions = new List<AdminGroupPermission> { new AdminGroupPermission { PermissionCode = "PERM1" } } } 
            }
        });

        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "DENY", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" }
        });

        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result); // Deny wins
    }

    [Fact]
    public async Task Evaluate_MultipleDepartments_AreUnioned()
    {
        SetupPermissions(new[] { 
            new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "COMPANY" },
            new Permission { PermissionCode = "PERM2", IsActive = true, DataScope = "COMPANY" }
        });
        SetupIndividualPermissions(new List<UserIndividualPermission>());
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        
        SetupDepartmentAssignments(new[] { 
            CreateAssignment(1, 10, 100, DateTime.MinValue),
            CreateAssignment(1, 11, 100, DateTime.MinValue)
        });
        SetupDepartmentPermissions(new[] {
            new DepartmentPermission { DepartmentId = 10, PermissionCode = "PERM1" },
            new DepartmentPermission { DepartmentId = 11, PermissionCode = "PERM2" }
        });

        var result1 = await _sut.EvaluateAsync(1, "PERM1", 100);
        var result2 = await _sut.EvaluateAsync(1, "PERM2", 100);
        
        Assert.True(result1);
        Assert.True(result2);
    }

    [Fact]
    public async Task Evaluate_CompanyPermission_MissingCompanyAssignment_ReturnsDeny()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "COMPANY" } });
        SetupIndividualPermissions(new List<UserIndividualPermission>());
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        
        SetupDepartmentAssignments(new[] { CreateAssignment(1, 10, 200, DateTime.MinValue) });
        SetupDepartmentPermissions(new[] {
            new DepartmentPermission { DepartmentId = 10, PermissionCode = "PERM1" }
        });

        // Requesting for Company 100 but user is only in Company 200
        var result = await _sut.EvaluateAsync(1, "PERM1", 100);
        Assert.False(result);
    }

    [Fact]
    public async Task Evaluate_GlobalPermission_RequiresNoCompany()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        // Providing company ID should fail because it's GLOBAL scope check
        var result = await _sut.EvaluateAsync(1, "PERM1", 100);
        Assert.False(result);
    }

    [Fact]
    public async Task Evaluate_CompanyPermission_RequiresCompany()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "COMPANY" } });
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "COMPANY", CompanyId = 100 }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        // Not providing company ID should fail because it's COMPANY scope check
        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result);
    }

    [Fact]
    public async Task Evaluate_InactivePermissionCatalogItem_NeverGrants()
    {
        // IsActive = false
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = false, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result);
    }

    [Fact]
    public async Task Evaluate_ExpiredAssignment_NeverGrants()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        // Expired!
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, EffectiveTo = DateTime.UtcNow.AddDays(-1), ScopeType = "GLOBAL" }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result);
    }

    [Fact]
    public async Task Evaluate_FutureDatedAssignment_NeverGrants()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        // Future!
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "SCHEDULED", EffectiveFrom = DateTime.UtcNow.AddDays(1), ScopeType = "GLOBAL" }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result);
    }

    [Fact]
    public async Task Evaluate_NoMatchingGrant_ReturnsDeny()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new List<UserIndividualPermission>());
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        
        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result);
    }

    [Fact]
    public async Task Evaluate_DataAccessException_ReturnsFailClosedDeny()
    {
        _dbContextMock.Setup(x => x.AuthorizationPolicyStates)
            .Throws(new Exception("DB Down"));
            
        var result = await _sut.EvaluateAsync(1, "PERM1", null);
        Assert.False(result);
    }
}
