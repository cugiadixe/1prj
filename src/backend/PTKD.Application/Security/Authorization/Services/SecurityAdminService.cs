using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Security.Authorization.DTOs;
using PTKD.Application.Security.Authorization.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authorization;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Security.Authorization.Services;

public sealed class SecurityAdminService : ISecurityAdminService
{
	private readonly IAuthorizationDbContext _db;

	private readonly TimeProvider _time;

	public SecurityAdminService(IAuthorizationDbContext db, TimeProvider time)
	{
		_db = db;
		_time = time;
	}

	public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await (from p in _db.Permissions.AsNoTracking()
			orderby p.ModuleCode, p.PermissionCode
			select p).ToListAsync(ct)).Select(MapPermission).ToList();
	}

	public async Task<PermissionDto> GetPermissionAsync(string code, CancellationToken ct = default(CancellationToken))
	{
		Permission perm = (await _db.Permissions.AsNoTracking().SingleOrDefaultAsync((Permission p) => p.PermissionCode == code, ct)) ?? throw new EntityNotFoundException("SEC_PERMISSION_NOT_FOUND", "Permission '" + code + "' not found.");
		return MapPermission(perm);
	}

	public async Task<IReadOnlyList<RoleDto>> ListRolesAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await (from r in _db.Roles.AsNoTracking().Include((Role r) => r.Permissions)
			orderby r.RoleCode
			select r).ToListAsync(ct)).Select(MapRole).ToList();
	}

	public async Task<RoleDto> GetRoleAsync(long id, CancellationToken ct = default(CancellationToken))
	{
		Role role = (await _db.Roles.AsNoTracking().Include((Role r) => r.Permissions).SingleOrDefaultAsync((Role r) => r.Id == id, ct)) ?? throw new EntityNotFoundException("SEC_ROLE_NOT_FOUND", $"Role {id} not found.");
		return MapRole(role);
	}

	public async Task<RoleDto> CreateRoleAsync(long actorUserId, CreateRoleRequest request, CancellationToken ct = default(CancellationToken))
	{
		ValidateScopeAndCompany(request.ScopeType, request.CompanyId);
		DateTime now = UtcNow();
		IExecutionStrategy strategy = _db.CreateExecutionStrategy();
		long roleId = 0L;
		await strategy.ExecuteAsync(async delegate
		{
			_db.ClearChangeTracker();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			Role role = new Role
			{
				RoleCode = request.RoleCode,
				Name = request.Name,
				Description = request.Description,
				ScopeType = request.ScopeType,
				CompanyId = request.CompanyId,
				IsActive = true,
				CreatedAt = now,
				CreatedByUserId = actorUserId
			};
			_db.Roles.Add(role);
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
			roleId = role.Id;
		});
		return await GetRoleAsync(roleId, ct);
	}

	public async Task<RoleDto> UpdateRoleAsync(long actorUserId, long id, UpdateRoleRequest request, CancellationToken ct = default(CancellationToken))
	{
		DateTime now = UtcNow();
		IExecutionStrategy strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async delegate
		{
			_db.ClearChangeTracker();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			Role role = (await _db.Roles.Include((Role r) => r.Permissions).SingleOrDefaultAsync((Role r) => r.Id == id, ct)) ?? throw new EntityNotFoundException("SEC_ROLE_NOT_FOUND", $"Role {id} not found.");
			RowVersion expectedRowVersion = RowVersion.FromBase64(request.RowVersion);
			if (!role.RowVersion.Equals(expectedRowVersion))
			{
				throw new ConcurrencyException("SEC_CONCURRENCY_ERROR", "Role has been modified by another process.");
			}
			role.Name = request.Name;
			role.Description = request.Description;
			role.UpdatedAt = now;
			role.UpdatedByUserId = actorUserId;
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
		return await GetRoleAsync(id, ct);
	}

	public async Task DeactivateRoleAsync(long actorUserId, long id, DeactivateRoleRequest request, CancellationToken ct = default(CancellationToken))
	{
		DateTime now = UtcNow();
		IExecutionStrategy strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async delegate
		{
			_db.ClearChangeTracker();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			Role role = (await _db.Roles.SingleOrDefaultAsync((Role r) => r.Id == id, ct)) ?? throw new EntityNotFoundException("SEC_ROLE_NOT_FOUND", $"Role {id} not found.");
			RowVersion expectedRowVersion = RowVersion.FromBase64(request.RowVersion);
			if (!role.RowVersion.Equals(expectedRowVersion))
			{
				throw new ConcurrencyException("SEC_CONCURRENCY_ERROR", "Role has been modified by another process.");
			}
			role.IsActive = false;
			role.UpdatedAt = now;
			role.UpdatedByUserId = actorUserId;
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task AddRolePermissionsAsync(long actorUserId, long roleId, AddRolePermissionsRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			Role role = (await _db.Roles.Include((Role r) => r.Permissions).SingleOrDefaultAsync((Role r) => r.Id == roleId, ct)) ?? throw new EntityNotFoundException("SEC_ROLE_NOT_FOUND", $"Role {roleId} not found.");
			if (!role.IsActive)
			{
				throw new InactiveEntityException("SEC_ROLE_INACTIVE", $"Role {roleId} is inactive.");
			}
			foreach (string code in request.PermissionCodes)
			{
				await RequireActivePermissionAsync(code, ct);
				if (!role.Permissions.Any((RolePermission p) => p.PermissionCode == code))
				{
					role.Permissions.Add(new RolePermission
					{
						RoleId = roleId,
						PermissionCode = code,
						CreatedAt = now,
						CreatedByUserId = actorUserId
					});
				}
			}
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task RemoveRolePermissionAsync(long actorUserId, long roleId, string permissionCode, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			Role role = (await _db.Roles.Include((Role r) => r.Permissions).SingleOrDefaultAsync((Role r) => r.Id == roleId, ct)) ?? throw new EntityNotFoundException("SEC_ROLE_NOT_FOUND", $"Role {roleId} not found.");
			RolePermission entry = role.Permissions.SingleOrDefault((RolePermission p) => p.PermissionCode == permissionCode);
			if (entry != null)
			{
				role.Permissions.Remove(entry);
				await IncrementPolicyVersionAsync(actorUserId, now, ct);
				await _db.SaveChangesAsync(ct);
				await tx.CommitAsync(ct);
			}
		});
	}

	public async Task<IReadOnlyList<AdminGroupDto>> ListAdminGroupsAsync(CancellationToken ct = default(CancellationToken))
	{
		return (await (from g in _db.AdminGroups.AsNoTracking().Include((AdminGroup g) => g.Permissions)
			orderby g.GroupCode
			select g).ToListAsync(ct)).Select(MapAdminGroup).ToList();
	}

	public async Task<AdminGroupDto> GetAdminGroupAsync(long id, CancellationToken ct = default(CancellationToken))
	{
		AdminGroup group = (await _db.AdminGroups.AsNoTracking().Include((AdminGroup g) => g.Permissions).SingleOrDefaultAsync((AdminGroup g) => g.Id == id, ct)) ?? throw new EntityNotFoundException("SEC_ADMIN_GROUP_NOT_FOUND", $"AdminGroup {id} not found.");
		return MapAdminGroup(group);
	}

	public async Task<AdminGroupDto> CreateAdminGroupAsync(long actorUserId, CreateAdminGroupRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		return await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			ValidateScopeAndCompany(request.ScopeType, request.CompanyId);
			DateTime now = UtcNow();
			AdminGroupDto result;
			await using (IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct))
			{
				AdminGroup group = new AdminGroup
				{
					GroupCode = request.GroupCode,
					Name = request.Name,
					Description = request.Description,
					ScopeType = request.ScopeType,
					CompanyId = request.CompanyId,
					IsActive = true,
					CreatedAt = now,
					CreatedByUserId = actorUserId
				};
				_db.AdminGroups.Add(group);
				await IncrementPolicyVersionAsync(actorUserId, now, ct);
				await _db.SaveChangesAsync(ct);
				await tx.CommitAsync(ct);
				result = await GetAdminGroupAsync(group.Id, ct);
			}
			return result;
		});
	}

	public async Task<AdminGroupDto> UpdateAdminGroupAsync(long actorUserId, long id, UpdateAdminGroupRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		return await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			AdminGroupDto result;
			await using (IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct))
			{
				AdminGroup group = (await _db.AdminGroups.Include((AdminGroup g) => g.Permissions).SingleOrDefaultAsync((AdminGroup g) => g.Id == id, ct)) ?? throw new EntityNotFoundException("SEC_ADMIN_GROUP_NOT_FOUND", $"AdminGroup {id} not found.");
				RowVersion expectedRowVersion = RowVersion.FromBase64(request.RowVersion);
				if (!group.RowVersion.Equals(expectedRowVersion))
				{
					throw new ConcurrencyException("SEC_CONCURRENCY_ERROR", "AdminGroup has been modified by another process.");
				}
				group.Name = request.Name;
				group.Description = request.Description;
				group.UpdatedAt = now;
				group.UpdatedByUserId = actorUserId;
				await IncrementPolicyVersionAsync(actorUserId, now, ct);
				await _db.SaveChangesAsync(ct);
				await tx.CommitAsync(ct);
				result = await GetAdminGroupAsync(group.Id, ct);
			}
			return result;
		});
	}

	public async Task DeactivateAdminGroupAsync(long actorUserId, long id, DeactivateAdminGroupRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			AdminGroup group = (await _db.AdminGroups.SingleOrDefaultAsync((AdminGroup g) => g.Id == id, ct)) ?? throw new EntityNotFoundException("SEC_ADMIN_GROUP_NOT_FOUND", $"AdminGroup {id} not found.");
			RowVersion expectedRowVersion = RowVersion.FromBase64(request.RowVersion);
			if (!group.RowVersion.Equals(expectedRowVersion))
			{
				throw new ConcurrencyException("SEC_CONCURRENCY_ERROR", "AdminGroup has been modified by another process.");
			}
			group.IsActive = false;
			group.UpdatedAt = now;
			group.UpdatedByUserId = actorUserId;
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task AddAdminGroupPermissionsAsync(long actorUserId, long groupId, AddAdminGroupPermissionsRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			AdminGroup group = (await _db.AdminGroups.Include((AdminGroup g) => g.Permissions).SingleOrDefaultAsync((AdminGroup g) => g.Id == groupId, ct)) ?? throw new EntityNotFoundException("SEC_ADMIN_GROUP_NOT_FOUND", $"AdminGroup {groupId} not found.");
			if (!group.IsActive)
			{
				throw new InactiveEntityException("SEC_ADMIN_GROUP_INACTIVE", $"AdminGroup {groupId} is inactive.");
			}
			foreach (string code in request.PermissionCodes)
			{
				await RequireActivePermissionAsync(code, ct);
				if (!group.Permissions.Any((AdminGroupPermission p) => p.PermissionCode == code))
				{
					group.Permissions.Add(new AdminGroupPermission
					{
						AdminGroupId = groupId,
						PermissionCode = code,
						CreatedAt = now,
						CreatedByUserId = actorUserId
					});
				}
			}
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task RemoveAdminGroupPermissionAsync(long actorUserId, long groupId, string permissionCode, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			AdminGroup group = (await _db.AdminGroups.Include((AdminGroup g) => g.Permissions).SingleOrDefaultAsync((AdminGroup g) => g.Id == groupId, ct)) ?? throw new EntityNotFoundException("SEC_ADMIN_GROUP_NOT_FOUND", $"AdminGroup {groupId} not found.");
			AdminGroupPermission entry = group.Permissions.SingleOrDefault((AdminGroupPermission p) => p.PermissionCode == permissionCode);
			if (entry != null)
			{
				group.Permissions.Remove(entry);
				await IncrementPolicyVersionAsync(actorUserId, now, ct);
				await _db.SaveChangesAsync(ct);
				await tx.CommitAsync(ct);
			}
		});
	}

	public async Task<IReadOnlyList<UserRoleAssignmentDto>> ListUserRoleAssignmentsAsync(long userId, CancellationToken ct = default(CancellationToken))
	{
		return (await (from a in _db.UserRoleAssignments.AsNoTracking().Include((UserRoleAssignment a) => a.Role)
			where a.UserId == userId
			orderby a.EffectiveFrom descending
			select a).ToListAsync(ct)).Select(MapUserRoleAssignment).ToList();
	}

	public async Task<(UserRoleAssignmentDto Assignment, bool WasIdempotent)> AssignRoleAsync(long actorUserId, long userId, CreateUserRoleAssignmentRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		return await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			// Normalize to datetime2(3) before any comparison or persistence to prevent
			// sub-millisecond tick mismatch between .NET and the SQL Server column.
			DateTime effectiveFrom = NormalizeEffectiveDate(request.EffectiveFrom);
			DateTime? effectiveTo = NormalizeEffectiveDateOrNull(request.EffectiveTo);
			Role role = (await _db.Roles.AsNoTracking().SingleOrDefaultAsync((Role r) => r.Id == request.RoleId, ct)) ?? throw new EntityNotFoundException("SEC_ROLE_NOT_FOUND", $"Role {request.RoleId} not found.");
			if (!role.IsActive)
			{
				throw new InactiveEntityException("SEC_ROLE_INACTIVE", $"Role {request.RoleId} is inactive.");
			}
			if (role.ScopeType == "COMPANY" && role.CompanyId.HasValue)
			{
				await RequireActiveCompanyAssignmentAsync(userId, role.CompanyId.Value, ct);
			}
			(UserRoleAssignmentDto Assignment, bool WasIdempotent) result;
			await using (IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct))
			{
				UserRoleAssignment existing = await _db.UserRoleAssignments.AsNoTracking().Include((UserRoleAssignment a) => a.Role).FirstOrDefaultAsync((UserRoleAssignment a) => a.UserId == userId && a.RoleId == request.RoleId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom == effectiveFrom && ((effectiveTo == null && a.EffectiveTo == null) || (effectiveTo != null && a.EffectiveTo == effectiveTo)), ct);
				if (existing != null)
				{
					await tx.RollbackAsync(ct);
					result = (Assignment: MapUserRoleAssignment(existing), WasIdempotent: true);
				}
				else
				{
					if (await _db.UserRoleAssignments.AsNoTracking().AnyAsync((UserRoleAssignment a) => a.UserId == userId && a.RoleId == request.RoleId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom < (effectiveTo ?? DateTime.MaxValue) && (a.EffectiveTo == null || a.EffectiveTo > effectiveFrom), ct))
					{
						await tx.RollbackAsync(ct);
						throw new BusinessRuleValidationException("SEC_ROLE_ASSIGNMENT_CONFLICT", $"An active overlapping role assignment already exists for user {userId} and role {request.RoleId}.");
					}
					UserRoleAssignment assignment = new UserRoleAssignment
					{
						UserId = userId,
						RoleId = request.RoleId,
						AssignmentStatus = "ACTIVE",
						EffectiveFrom = effectiveFrom,
						EffectiveTo = effectiveTo,
						CreatedAt = now,
						CreatedByUserId = actorUserId
					};
					_db.UserRoleAssignments.Add(assignment);
					await IncrementPolicyVersionAsync(actorUserId, now, ct);
					await _db.SaveChangesAsync(ct);
					await tx.CommitAsync(ct);
					result = (Assignment: MapUserRoleAssignment(await _db.UserRoleAssignments.AsNoTracking().Include((UserRoleAssignment a) => a.Role).SingleAsync((UserRoleAssignment a) => a.Id == assignment.Id, ct)), WasIdempotent: false);
				}
			}
			return result;
		});
	}

	public async Task DeactivateUserRoleAssignmentAsync(long actorUserId, long userId, long assignmentId, DeactivateAssignmentRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			UserRoleAssignment assignment = (await _db.UserRoleAssignments.SingleOrDefaultAsync((UserRoleAssignment a) => a.Id == assignmentId && a.UserId == userId, ct)) ?? throw new EntityNotFoundException("SEC_ROLE_ASSIGNMENT_NOT_FOUND", $"Role assignment {assignmentId} not found.");
			RowVersion expectedRowVersion = RowVersion.FromBase64(request.RowVersion);
			if (!assignment.RowVersion.Equals(expectedRowVersion))
			{
				throw new ConcurrencyException("SEC_CONCURRENCY_ERROR", "Role assignment has been modified by another process.");
			}
			assignment.AssignmentStatus = "INACTIVE";
			assignment.EffectiveTo = now;
			assignment.UpdatedAt = now;
			assignment.UpdatedByUserId = actorUserId;
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task<IReadOnlyList<UserAdminGroupAssignmentDto>> ListUserAdminGroupAssignmentsAsync(long userId, CancellationToken ct = default(CancellationToken))
	{
		return (await (from a in _db.UserAdminGroupAssignments.AsNoTracking().Include((UserAdminGroupAssignment a) => a.AdminGroup)
			where a.UserId == userId
			orderby a.EffectiveFrom descending
			select a).ToListAsync(ct)).Select(MapUserAdminGroupAssignment).ToList();
	}

	public async Task<(UserAdminGroupAssignmentDto Assignment, bool WasIdempotent)> AssignAdminGroupAsync(long actorUserId, long userId, CreateUserAdminGroupAssignmentRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		return await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			// Normalize to datetime2(3) before any comparison or persistence.
			DateTime effectiveFrom = NormalizeEffectiveDate(request.EffectiveFrom);
			DateTime? effectiveTo = NormalizeEffectiveDateOrNull(request.EffectiveTo);
			AdminGroup group = (await _db.AdminGroups.AsNoTracking().SingleOrDefaultAsync((AdminGroup g) => g.Id == request.AdminGroupId, ct)) ?? throw new EntityNotFoundException("SEC_ADMIN_GROUP_NOT_FOUND", $"AdminGroup {request.AdminGroupId} not found.");
			if (!group.IsActive)
			{
				throw new InactiveEntityException("SEC_ADMIN_GROUP_INACTIVE", $"AdminGroup {request.AdminGroupId} is inactive.");
			}
			if (group.ScopeType == "COMPANY" && group.CompanyId.HasValue)
			{
				await RequireActiveCompanyAssignmentAsync(userId, group.CompanyId.Value, ct);
			}
			(UserAdminGroupAssignmentDto Assignment, bool WasIdempotent) result;
			await using (IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct))
			{
				UserAdminGroupAssignment existing = await _db.UserAdminGroupAssignments.AsNoTracking().Include((UserAdminGroupAssignment a) => a.AdminGroup).FirstOrDefaultAsync((UserAdminGroupAssignment a) => a.UserId == userId && a.AdminGroupId == request.AdminGroupId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom == effectiveFrom && ((effectiveTo == null && a.EffectiveTo == null) || (effectiveTo != null && a.EffectiveTo == effectiveTo)), ct);
				if (existing != null)
				{
					await tx.RollbackAsync(ct);
					result = (Assignment: MapUserAdminGroupAssignment(existing), WasIdempotent: true);
				}
				else
				{
					if (await _db.UserAdminGroupAssignments.AsNoTracking().AnyAsync((UserAdminGroupAssignment a) => a.UserId == userId && a.AdminGroupId == request.AdminGroupId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom < (effectiveTo ?? DateTime.MaxValue) && (a.EffectiveTo == null || a.EffectiveTo > effectiveFrom), ct))
					{
						await tx.RollbackAsync(ct);
						throw new BusinessRuleValidationException("SEC_ADMIN_GROUP_ASSIGNMENT_CONFLICT", $"An active overlapping admin group assignment already exists for user {userId} and group {request.AdminGroupId}.");
					}
					UserAdminGroupAssignment assignment = new UserAdminGroupAssignment
					{
						UserId = userId,
						AdminGroupId = request.AdminGroupId,
						AssignmentStatus = "ACTIVE",
						EffectiveFrom = effectiveFrom,
						EffectiveTo = effectiveTo,
						CreatedAt = now,
						CreatedByUserId = actorUserId
					};
					_db.UserAdminGroupAssignments.Add(assignment);
					await IncrementPolicyVersionAsync(actorUserId, now, ct);
					await _db.SaveChangesAsync(ct);
					await tx.CommitAsync(ct);
					result = (Assignment: MapUserAdminGroupAssignment(await _db.UserAdminGroupAssignments.AsNoTracking().Include((UserAdminGroupAssignment a) => a.AdminGroup).SingleAsync((UserAdminGroupAssignment a) => a.Id == assignment.Id, ct)), WasIdempotent: false);
				}
			}
			return result;
		});
	}

	public async Task DeactivateUserAdminGroupAssignmentAsync(long actorUserId, long userId, long assignmentId, DeactivateAssignmentRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			UserAdminGroupAssignment assignment = (await _db.UserAdminGroupAssignments.SingleOrDefaultAsync((UserAdminGroupAssignment a) => a.Id == assignmentId && a.UserId == userId, ct)) ?? throw new EntityNotFoundException("SEC_ADMIN_GROUP_ASSIGNMENT_NOT_FOUND", $"AdminGroup assignment {assignmentId} not found.");
			RowVersion expectedRowVersion = RowVersion.FromBase64(request.RowVersion);
			if (!assignment.RowVersion.Equals(expectedRowVersion))
			{
				throw new ConcurrencyException("SEC_CONCURRENCY_ERROR", "AdminGroup assignment has been modified by another process.");
			}
			assignment.AssignmentStatus = "INACTIVE";
			assignment.EffectiveTo = now;
			assignment.UpdatedAt = now;
			assignment.UpdatedByUserId = actorUserId;
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task<IReadOnlyList<UserIndividualPermissionDto>> ListUserIndividualPermissionsAsync(long userId, CancellationToken ct = default(CancellationToken))
	{
		return (await (from p in _db.UserIndividualPermissions.AsNoTracking()
			where p.UserId == userId
			orderby p.EffectiveFrom descending
			select p).ToListAsync(ct)).Select(MapUserIndividualPermission).ToList();
	}

	public async Task<(UserIndividualPermissionDto Permission, bool WasIdempotent)> GrantIndividualPermissionAsync(long actorUserId, long userId, CreateUserIndividualPermissionRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		return await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			// Normalize to datetime2(3) before any comparison or persistence.
			DateTime effectiveFrom = NormalizeEffectiveDate(request.EffectiveFrom);
			DateTime? effectiveTo = NormalizeEffectiveDateOrNull(request.EffectiveTo);
			await RequireActivePermissionAsync(request.PermissionCode, ct);
			if (request.ScopeType == "COMPANY" && request.CompanyId.HasValue)
			{
				await RequireActiveCompanyAssignmentAsync(userId, request.CompanyId.Value, ct);
			}
			(UserIndividualPermissionDto Permission, bool WasIdempotent) result;
			await using (IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct))
			{
				UserIndividualPermission existing = await _db.UserIndividualPermissions.AsNoTracking().FirstOrDefaultAsync((UserIndividualPermission p) => p.UserId == userId && p.PermissionCode == request.PermissionCode && p.ScopeType == request.ScopeType && p.CompanyId == request.CompanyId && p.GrantType == request.GrantType && p.AssignmentStatus == "ACTIVE" && p.EffectiveFrom == effectiveFrom && ((effectiveTo == null && p.EffectiveTo == null) || (effectiveTo != null && p.EffectiveTo == effectiveTo)), ct);
				if (existing != null)
				{
					await tx.RollbackAsync(ct);
					result = (Permission: MapUserIndividualPermission(existing), WasIdempotent: true);
				}
				else
				{
					if (await _db.UserIndividualPermissions.AsNoTracking().AnyAsync((UserIndividualPermission p) => p.UserId == userId && p.PermissionCode == request.PermissionCode && p.ScopeType == request.ScopeType && p.CompanyId == request.CompanyId && p.GrantType == request.GrantType && p.AssignmentStatus == "ACTIVE" && p.EffectiveFrom < (effectiveTo ?? DateTime.MaxValue) && (p.EffectiveTo == null || p.EffectiveTo > effectiveFrom), ct))
					{
						await tx.RollbackAsync(ct);
						throw new BusinessRuleValidationException("SEC_INDIVIDUAL_PERMISSION_CONFLICT", $"An active overlapping individual permission already exists for user {userId} and permission {request.PermissionCode}.");
					}
					UserIndividualPermission perm = new UserIndividualPermission
					{
						UserId = userId,
						PermissionCode = request.PermissionCode,
						ScopeType = request.ScopeType,
						CompanyId = request.CompanyId,
						GrantType = request.GrantType,
						AssignmentStatus = "ACTIVE",
						EffectiveFrom = effectiveFrom,
						EffectiveTo = effectiveTo,
						Reason = request.Reason,
						CreatedAt = now,
						CreatedByUserId = actorUserId
					};
					_db.UserIndividualPermissions.Add(perm);
					await IncrementPolicyVersionAsync(actorUserId, now, ct);
					await _db.SaveChangesAsync(ct);
					await tx.CommitAsync(ct);
					result = (Permission: MapUserIndividualPermission(await _db.UserIndividualPermissions.AsNoTracking().SingleAsync((UserIndividualPermission p) => p.Id == perm.Id, ct)), WasIdempotent: false);
				}
			}
			return result;
		});
	}

	public async Task DeactivateIndividualPermissionAsync(long actorUserId, long userId, long permissionId, DeactivateAssignmentRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			UserIndividualPermission perm = (await _db.UserIndividualPermissions.SingleOrDefaultAsync((UserIndividualPermission p) => p.Id == permissionId && p.UserId == userId, ct)) ?? throw new EntityNotFoundException("SEC_INDIVIDUAL_PERMISSION_NOT_FOUND", $"Individual permission {permissionId} not found.");
			RowVersion expectedRowVersion = RowVersion.FromBase64(request.RowVersion);
			if (!perm.RowVersion.Equals(expectedRowVersion))
			{
				throw new ConcurrencyException("SEC_CONCURRENCY_ERROR", "Individual permission has been modified by another process.");
			}
			perm.AssignmentStatus = "INACTIVE";
			perm.EffectiveTo = now;
			perm.UpdatedAt = now;
			perm.UpdatedByUserId = actorUserId;
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task<IReadOnlyList<DepartmentPermissionDto>> ListDepartmentPermissionsAsync(long departmentId, CancellationToken ct = default(CancellationToken))
	{
		return (await (from p in _db.DepartmentPermissions.AsNoTracking()
			where p.DepartmentId == departmentId
			orderby p.PermissionCode
			select p).ToListAsync(ct)).Select((DepartmentPermission p) => new DepartmentPermissionDto(p.DepartmentId, p.PermissionCode)).ToList();
	}

	public async Task SetDepartmentPermissionsAsync(long actorUserId, long departmentId, SetDepartmentPermissionsRequest request, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			foreach (string code in request.PermissionCodes)
			{
				await RequireActivePermissionAsync(code, ct);
			}
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			List<DepartmentPermission> existing = await _db.DepartmentPermissions.Where((DepartmentPermission p) => p.DepartmentId == departmentId).ToListAsync(ct);
			HashSet<string> existingCodes = existing.Select((DepartmentPermission p) => p.PermissionCode).ToHashSet();
			HashSet<string> requestedCodes = request.PermissionCodes.ToHashSet();
			foreach (string code2 in requestedCodes.Except(existingCodes))
			{
				_db.DepartmentPermissions.Add(new DepartmentPermission
				{
					DepartmentId = departmentId,
					PermissionCode = code2,
					CreatedAt = now,
					CreatedByUserId = actorUserId
				});
			}
			foreach (DepartmentPermission entry in existing.Where((DepartmentPermission p) => !requestedCodes.Contains(p.PermissionCode)))
			{
				_db.DepartmentPermissions.Remove(entry);
			}
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task RemoveDepartmentPermissionAsync(long actorUserId, long departmentId, string permissionCode, CancellationToken ct = default(CancellationToken))
	{
		var strategy = _db.CreateExecutionStrategy();
		await strategy.ExecuteAsync(async ()	=>
		{
			_db.ClearChangeTracker();
			DateTime now = UtcNow();
			await using IDbContextTransaction tx = await _db.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
			DepartmentPermission entry = await _db.DepartmentPermissions.SingleOrDefaultAsync((DepartmentPermission p) => p.DepartmentId == departmentId && p.PermissionCode == permissionCode, ct);
			if (entry == null)
			{
				await tx.RollbackAsync(ct);
				return;
			}
			_db.DepartmentPermissions.Remove(entry);
			await IncrementPolicyVersionAsync(actorUserId, now, ct);
			await _db.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		});
	}

	public async Task<EffectivePermissionsResponse> GetEffectivePermissionsAsync(long userId, long? companyId, CancellationToken ct = default(CancellationToken))
	{
		DateTime now = UtcNow();
		HashSet<string> activePermSet = new HashSet<string>(await (from permission in _db.Permissions.AsNoTracking()
			where permission.IsActive
			select permission.PermissionCode).ToListAsync(ct));
		HashSet<string> grantedSet = new HashSet<string>();
		if (companyId.HasValue)
		{
			List<long> activeDepts = await (from a in _db.UserDepartmentAssignments.AsNoTracking()
				where a.UserId == userId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom <= now && (a.EffectiveTo == null || a.EffectiveTo > now)
				select a.DepartmentId).ToListAsync(ct);
			if (activeDepts.Count > 0)
			{
				foreach (string p in await (from departmentPermission in _db.DepartmentPermissions.AsNoTracking()
					where activeDepts.Contains(departmentPermission.DepartmentId)
					select departmentPermission.PermissionCode).ToListAsync(ct))
				{
					grantedSet.Add(p);
				}
			}
		}
		foreach (string p2 in await (from rolePermission in (from a in _db.UserRoleAssignments.AsNoTracking()
				where a.UserId == userId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom <= now && (a.EffectiveTo == null || a.EffectiveTo > now) && a.Role.IsActive && (a.Role.ScopeType == "GLOBAL" || a.Role.CompanyId == companyId)
				select a).SelectMany((UserRoleAssignment a) => a.Role.Permissions)
			select rolePermission.PermissionCode).ToListAsync(ct))
		{
			grantedSet.Add(p2);
		}
		foreach (string p3 in await (from userIndividualPermission in _db.UserIndividualPermissions.AsNoTracking()
			where userIndividualPermission.UserId == userId && userIndividualPermission.AssignmentStatus == "ACTIVE" && userIndividualPermission.GrantType == "ALLOW" && userIndividualPermission.EffectiveFrom <= now && (userIndividualPermission.EffectiveTo == null || userIndividualPermission.EffectiveTo > now) && (userIndividualPermission.ScopeType == "GLOBAL" || userIndividualPermission.CompanyId == companyId)
			select userIndividualPermission.PermissionCode).ToListAsync(ct))
		{
			grantedSet.Add(p3);
		}
		foreach (string p4 in await (from adminGroupPermission in (from a in _db.UserAdminGroupAssignments.AsNoTracking()
				where a.UserId == userId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom <= now && (a.EffectiveTo == null || a.EffectiveTo > now) && a.AdminGroup.IsActive && (a.AdminGroup.ScopeType == "GLOBAL" || a.AdminGroup.CompanyId == companyId)
				select a).SelectMany((UserAdminGroupAssignment a) => a.AdminGroup.Permissions)
			select adminGroupPermission.PermissionCode).ToListAsync(ct))
		{
			grantedSet.Add(p4);
		}
		foreach (string p5 in await (from userIndividualPermission in _db.UserIndividualPermissions.AsNoTracking()
			where userIndividualPermission.UserId == userId && userIndividualPermission.AssignmentStatus == "ACTIVE" && userIndividualPermission.GrantType == "DENY" && userIndividualPermission.EffectiveFrom <= now && (userIndividualPermission.EffectiveTo == null || userIndividualPermission.EffectiveTo > now) && (userIndividualPermission.ScopeType == "GLOBAL" || userIndividualPermission.CompanyId == companyId)
			select userIndividualPermission.PermissionCode).ToListAsync(ct))
		{
			grantedSet.Remove(p5);
		}
		grantedSet.IntersectWith(activePermSet);
		return new EffectivePermissionsResponse(userId, companyId, grantedSet.OrderBy((string result) => result).ToList());
	}

	private DateTime UtcNow()
	{
		return NormalizeEffectiveDate(_time.GetUtcNow().UtcDateTime);
	}

	/// <summary>
	/// Truncates a DateTime to datetime2(3) precision (milliseconds) to match the database column type.
	/// All three assignment tables use datetime2(3): User_Role_Assignments, User_Admin_Group_Assignments,
	/// User_Individual_Permissions. Without normalization, .NET sub-millisecond ticks are silently
	/// dropped by SQL Server, causing idempotency equality checks to miss an existing record.
	/// </summary>
	private static DateTime NormalizeEffectiveDate(DateTime value)
	{
		// datetime2(3) stores milliseconds; discard sub-millisecond ticks.
		return new DateTime(
			value.Year, value.Month, value.Day,
			value.Hour, value.Minute, value.Second,
			value.Millisecond,
			value.Kind);
	}

	/// <summary>Nullable overload of <see cref="NormalizeEffectiveDate"/>.</summary>
	private static DateTime? NormalizeEffectiveDateOrNull(DateTime? value)
		=> value.HasValue ? NormalizeEffectiveDate(value.Value) : null;

	private async Task IncrementPolicyVersionAsync(long actorUserId, DateTime now, CancellationToken ct)
	{
		AuthorizationPolicyState state = await _db.AuthorizationPolicyStates.SingleOrDefaultAsync((AuthorizationPolicyState p) => p.Id == 1, ct);
		if (state == null)
		{
			_db.AuthorizationPolicyStates.Add(new AuthorizationPolicyState
			{
				Id = 1,
				PolicyVersion = 1L,
				UpdatedAt = now,
				UpdatedByUserId = actorUserId
			});
		}
		else
		{
			state.PolicyVersion++;
			state.UpdatedAt = now;
			state.UpdatedByUserId = actorUserId;
		}
	}

	private static void ValidateScopeAndCompany(string scopeType, long? companyId)
	{
		if (scopeType == "GLOBAL" && companyId.HasValue)
		{
			throw new BusinessRuleValidationException("SEC_SCOPE_COMPANY_MISMATCH", "GLOBAL scope must not specify a CompanyId.");
		}
		if (scopeType == "COMPANY" && !companyId.HasValue)
		{
			throw new BusinessRuleValidationException("SEC_SCOPE_COMPANY_REQUIRED", "COMPANY scope requires a CompanyId.");
		}
	}

	private async Task RequireActivePermissionAsync(string code, CancellationToken ct)
	{
		if (!(await _db.Permissions.AsNoTracking().AnyAsync((Permission p) => p.PermissionCode == code && p.IsActive, ct)))
		{
			throw new InactiveEntityException("SEC_PERMISSION_INACTIVE", "Permission '" + code + "' does not exist or is inactive.");
		}
	}

	private async Task RequireActiveCompanyAssignmentAsync(long userId, long companyId, CancellationToken ct)
	{
		DateTime now = UtcNow();
		if (!(await _db.UserCompanyAssignments.AsNoTracking().AnyAsync((UserCompanyAssignment a) => a.UserId == userId && a.CompanyId == companyId && a.AssignmentStatus == "ACTIVE" && a.EffectiveFrom <= now && (a.EffectiveTo == null || a.EffectiveTo > now), ct)))
		{
			throw new BusinessRuleValidationException("SEC_USER_COMPANY_NOT_ASSIGNED", $"User {userId} does not have an active company assignment for company {companyId}.");
		}
	}

	private static PermissionDto MapPermission(Permission p)
	{
		return new PermissionDto(p.PermissionCode, p.ModuleCode, p.ActionCode, p.DataScope, p.IsSensitive, p.IsDelegable, p.RequiresReason, p.IsActive, p.Description);
	}

	private static RoleDto MapRole(Role r)
	{
		return new RoleDto(r.Id, r.RoleCode, r.Name, r.Description, r.ScopeType, r.CompanyId, r.IsActive, (from p in r.Permissions
			select p.PermissionCode into c
			orderby c
			select c).ToList(), r.RowVersion.ToBase64());
	}

	private static AdminGroupDto MapAdminGroup(AdminGroup g)
	{
		return new AdminGroupDto(g.Id, g.GroupCode, g.Name, g.Description, g.ScopeType, g.CompanyId, g.IsActive, (from p in g.Permissions
			select p.PermissionCode into c
			orderby c
			select c).ToList(), g.RowVersion.ToBase64());
	}

	private static UserRoleAssignmentDto MapUserRoleAssignment(UserRoleAssignment a)
	{
		return new UserRoleAssignmentDto(a.Id, a.UserId, a.RoleId, a.Role?.RoleCode ?? string.Empty, a.Role?.Name ?? string.Empty, a.AssignmentStatus, a.EffectiveFrom, a.EffectiveTo, a.RowVersion.ToBase64());
	}

	private static UserAdminGroupAssignmentDto MapUserAdminGroupAssignment(UserAdminGroupAssignment a)
	{
		return new UserAdminGroupAssignmentDto(a.Id, a.UserId, a.AdminGroupId, a.AdminGroup?.GroupCode ?? string.Empty, a.AdminGroup?.Name ?? string.Empty, a.AssignmentStatus, a.EffectiveFrom, a.EffectiveTo, a.RowVersion.ToBase64());
	}

	private static UserIndividualPermissionDto MapUserIndividualPermission(UserIndividualPermission p)
	{
		return new UserIndividualPermissionDto(p.Id, p.UserId, p.PermissionCode, p.ScopeType, p.CompanyId, p.GrantType, p.AssignmentStatus, p.EffectiveFrom, p.EffectiveTo, p.Reason, p.RowVersion.ToBase64());
	}
}
