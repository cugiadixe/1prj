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

    /// <summary>
    /// Lần cấp ở phạm vi TOÀN CỤC phải dùng được ở MỌI công ty.
    ///
    /// Test này trước đây khẳng định điều ngược lại (cấp toàn cục mà hỏi kèm công ty thì trượt),
    /// vì mô hình cũ dùng cột data_scope của DANH MỤC để chặn cứng. Hệ quả là quyền khai GLOBAL
    /// chỉ có hai nấc — mất sạch, hoặc thấy hết — không có nấc "chỉ công ty mình".
    /// Phạm vi nay là thuộc tính của LẦN CẤP, và data_scope không còn tham gia quyết định.
    /// </summary>
    [Fact]
    public async Task Evaluate_GlobalGrant_AppliesToEveryCompany()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());

        Assert.True(await _sut.EvaluateAsync(1, "PERM1", 100));
        Assert.True(await _sut.EvaluateAsync(1, "PERM1", 999));
        Assert.True(await _sut.EvaluateAsync(1, "PERM1", null));

        var scope = await _sut.ResolveAsync(1, "PERM1");
        Assert.True(scope.Granted);
        Assert.True(scope.IsUnrestricted);
    }

    /// <summary>
    /// Lần cấp theo CÔNG TY chỉ có tác dụng ở đúng công ty đó, và
    /// <see cref="PermissionScopeResult.AllowedCompanyIds"/> phải nói rõ là công ty nào để nơi
    /// gọi lọc dữ liệu — đây là thứ mô hình cũ (chỉ trả bool) không diễn đạt được.
    /// </summary>
    [Fact]
    public async Task Resolve_CompanyGrant_ReportsExactCompanies()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "COMPANY", CompanyId = 100 },
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "COMPANY", CompanyId = 300 }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());

        var scope = await _sut.ResolveAsync(1, "PERM1");

        Assert.True(scope.Granted);
        Assert.False(scope.IsUnrestricted);
        Assert.Equal(new long[] { 100, 300 }, scope.AllowedCompanyIds);
        Assert.True(scope.Allows(100));
        Assert.False(scope.Allows(200));
    }

    /// <summary>
    /// Lệnh CẤM theo công ty phải cắn được cả người được cấp TOÀN CỤC.
    /// Mô hình cũ không làm được: DENY phạm vi công ty hoàn toàn câm với quyền khai GLOBAL —
    /// cấm mà không cấm được.
    /// </summary>
    [Fact]
    public async Task Resolve_CompanyDeny_BitesGlobalGrant()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" },
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "DENY", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "COMPANY", CompanyId = 200 }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());

        var scope = await _sut.ResolveAsync(1, "PERM1");

        Assert.True(scope.Granted);
        Assert.True(scope.IsGlobal);
        Assert.Contains(200L, scope.ExcludedCompanyIds);
        Assert.False(scope.Allows(200));
        Assert.True(scope.Allows(100));
        Assert.False(await _sut.EvaluateAsync(1, "PERM1", 200));
    }

    /// <summary>
    /// Quyền chuẩn của phòng ban chỉ áp cho công ty CỦA PHÒNG ĐÓ.
    /// Mô hình cũ bỏ lọc công ty ở nhánh này khi không có ngữ cảnh công ty, biến "gán vào phòng
    /// ban = có quyền" thành "gán vào phòng ban bất kỳ = có quyền ở mọi công ty".
    /// </summary>
    [Fact]
    public async Task Resolve_DepartmentBaseline_IsScopedToDepartmentCompany()
    {
        SetupPermissions(new[] { new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" } });
        SetupIndividualPermissions(new List<UserIndividualPermission>());
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new[] { CreateAssignment(1, 10, 200, DateTime.MinValue) });
        SetupDepartmentPermissions(new[] {
            new DepartmentPermission { DepartmentId = 10, PermissionCode = "PERM1" }
        });

        var scope = await _sut.ResolveAsync(1, "PERM1");

        Assert.True(scope.Granted);
        Assert.False(scope.IsGlobal);
        Assert.Equal(new long[] { 200 }, scope.AllowedCompanyIds);
        Assert.False(scope.Allows(100));
        // Không có ngữ cảnh công ty thì quyền từ phòng ban KHÔNG tự thành quyền toàn cục.
        Assert.False(await _sut.EvaluateAsync(1, "PERM1", null));
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
    [Fact]
    public async Task GetEffectivePermissionsAsync_ReturnsUnionOfAllSources()
    {
        SetupPermissions(new[] { 
            new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" },
            new Permission { PermissionCode = "PERM2", IsActive = true, DataScope = "GLOBAL" },
            new Permission { PermissionCode = "PERM3", IsActive = true, DataScope = "COMPANY" },
            new Permission { PermissionCode = "PERM4", IsActive = true, DataScope = "COMPANY" }
        });

        // 1. Department baseline -> PERM3 (Company 100)
        SetupDepartmentAssignments(new[] { CreateAssignment(1, 10, 100, DateTime.MinValue) });
        SetupDepartmentPermissions(new[] { new DepartmentPermission { DepartmentId = 10, PermissionCode = "PERM3" } });

        // 2. Role Grant -> PERM1
        SetupRoleAssignments(new[] {
            new UserRoleAssignment { 
                UserId = 1, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, 
                Role = new Role { Id = 20, IsActive = true, ScopeType = "GLOBAL", Permissions = new List<RolePermission> { new RolePermission { PermissionCode = "PERM1" } } } 
            }
        });

        // 3. Admin Group Grant -> PERM2
        SetupAdminGroupAssignments(new[] {
            new UserAdminGroupAssignment { 
                UserId = 1, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, 
                AdminGroup = new AdminGroup { Id = 30, IsActive = true, ScopeType = "GLOBAL", Permissions = new List<AdminGroupPermission> { new AdminGroupPermission { PermissionCode = "PERM2" } } } 
            }
        });

        // 4. Individual Allow -> PERM4 (Company 100)
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM4", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "COMPANY", CompanyId = 100 }
        });

        var results = await _sut.GetEffectivePermissionsAsync(1, 100);

        Assert.Equal(4, results.Count);
        Assert.Contains("PERM1", results);
        Assert.Contains("PERM2", results);
        Assert.Contains("PERM3", results);
        Assert.Contains("PERM4", results);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_SubtractsIndividualDeny()
    {
        SetupPermissions(new[] { 
            new Permission { PermissionCode = "PERM1", IsActive = true, DataScope = "GLOBAL" },
            new Permission { PermissionCode = "PERM2", IsActive = true, DataScope = "GLOBAL" }
        });

        // Granted PERM1 & PERM2 via Admin Group
        SetupAdminGroupAssignments(new[] {
            new UserAdminGroupAssignment { 
                UserId = 1, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, 
                AdminGroup = new AdminGroup { Id = 30, IsActive = true, ScopeType = "GLOBAL", Permissions = new List<AdminGroupPermission> { 
                    new AdminGroupPermission { PermissionCode = "PERM1" },
                    new AdminGroupPermission { PermissionCode = "PERM2" }
                } } 
            }
        });
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());

        // Deny PERM1
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM1", GrantType = "DENY", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" }
        });

        var results = await _sut.GetEffectivePermissionsAsync(1, null);

        Assert.Single(results);
        Assert.Contains("PERM2", results);
        Assert.DoesNotContain("PERM1", results);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_ExcludesInactivePermissions()
    {
        SetupPermissions(new[] { 
            new Permission { PermissionCode = "PERM1", IsActive = false, DataScope = "GLOBAL" }, // Inactive!
            new Permission { PermissionCode = "PERM2", IsActive = true, DataScope = "GLOBAL" }
        });

        SetupRoleAssignments(new[] {
            new UserRoleAssignment { 
                UserId = 1, AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, 
                Role = new Role { Id = 20, IsActive = true, ScopeType = "GLOBAL", Permissions = new List<RolePermission> { 
                    new RolePermission { PermissionCode = "PERM1" },
                    new RolePermission { PermissionCode = "PERM2" }
                } } 
            }
        });
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());
        SetupDepartmentAssignments(new List<UserDepartmentAssignment>());
        SetupIndividualPermissions(new List<UserIndividualPermission>());

        var results = await _sut.GetEffectivePermissionsAsync(1, null);

        Assert.Single(results);
        Assert.Contains("PERM2", results);
        Assert.DoesNotContain("PERM1", results);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_ReturnsEmptyOnException()
    {
        _dbContextMock.Setup(x => x.AuthorizationPolicyStates)
            .Throws(new Exception("DB Down"));
            
        var results = await _sut.GetEffectivePermissionsAsync(1, null);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetEffectivePermissionsAsync_RespectsCompanyScope()
    {
        SetupPermissions(new[] { 
            new Permission { PermissionCode = "PERM_CO_100", IsActive = true, DataScope = "COMPANY" },
            new Permission { PermissionCode = "PERM_CO_200", IsActive = true, DataScope = "COMPANY" },
            new Permission { PermissionCode = "PERM_GLOBAL", IsActive = true, DataScope = "GLOBAL" }
        });

        // User is in Company 100 with PERM_CO_100
        SetupDepartmentAssignments(new[] { 
            CreateAssignment(1, 10, 100, DateTime.MinValue),
            CreateAssignment(1, 11, 200, DateTime.MinValue) 
        });
        SetupDepartmentPermissions(new[] { 
            new DepartmentPermission { DepartmentId = 10, PermissionCode = "PERM_CO_100" },
            new DepartmentPermission { DepartmentId = 11, PermissionCode = "PERM_CO_200" }
        });

        // Global Allow
        SetupIndividualPermissions(new[] {
            new UserIndividualPermission { UserId = 1, PermissionCode = "PERM_GLOBAL", GrantType = "ALLOW", AssignmentStatus = "ACTIVE", EffectiveFrom = DateTime.MinValue, ScopeType = "GLOBAL" }
        });
        SetupRoleAssignments(new List<UserRoleAssignment>());
        SetupAdminGroupAssignments(new List<UserAdminGroupAssignment>());

        // Request permissions for Company 100 context
        var results = await _sut.GetEffectivePermissionsAsync(1, 100);

        Assert.Equal(2, results.Count);
        Assert.Contains("PERM_GLOBAL", results);
        Assert.Contains("PERM_CO_100", results);
        Assert.DoesNotContain("PERM_CO_200", results);
    }
}
