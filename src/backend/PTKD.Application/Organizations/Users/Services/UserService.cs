using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Organizations.Users.DTOs;
using PTKD.Domain.Entities;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Organizations.Users.Services;

public class UserService : IUserService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public UserService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            if (await context.Users.AnyAsync(u => u.EmployeeCode == request.EmployeeCode))
            {
                throw new BusinessRuleValidationException("ORG_DUPLICATE_EMPLOYEE_CODE", "Employee code already exists.");
            }

            var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == request.InitialCompanyId);
            if (company == null || !company.IsActive)
            {
                throw new BusinessRuleValidationException("ORG_COMPANY_NOT_FOUND", "Initial company not found or inactive.");
            }

            var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.InitialDepartmentId);
            if (department == null || !department.IsActive || department.CompanyId != request.InitialCompanyId)
            {
                throw new BusinessRuleValidationException("ORG_DEPARTMENT_NOT_FOUND", "Initial department not found, inactive, or belongs to another company.");
            }

            var user = new User(request.EmployeeCode, request.FullName, request.Email, request.EmploymentStatus, request.AccountStatus);
            context.Users.Add(user);
            await context.SaveChangesAsync(); 

            var companyAssignment = new UserCompanyAssignment(user.Id, company.Id, true, request.EffectiveFrom);
            context.UserCompanyAssignments.Add(companyAssignment);
            await context.SaveChangesAsync();

            var departmentAssignment = new UserDepartmentAssignment(user.Id, department.Id, companyAssignment.Id, company.Id, true, request.EffectiveFrom);
            context.UserDepartmentAssignments.Add(departmentAssignment);
            await context.SaveChangesAsync();

            var history = new EmploymentHistory(
                userId: user.Id,
                actionType: "HIRED",
                effectiveDate: request.EffectiveFrom,
                reason: request.Reason,
                toCompanyId: company.Id,
                toDepartmentId: department.Id,
                toCompanyAssignmentId: companyAssignment.Id,
                toDepartmentAssignmentId: departmentAssignment.Id
            );
            context.EmploymentHistories.Add(history);
            
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToDto(user);
        });
    }

    public async Task<UserDto> UpdateUserAsync(long id, UpdateUserRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;
        
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                throw new EntityNotFoundException("ORG_USER_NOT_FOUND", "User not found.");
            }

            if (!user.RowVersion.SequenceEqual(rowVersion))
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The user has been modified by another process.");
            }

            if (user.EmployeeCode != request.EmployeeCode)
            {
                if (await context.Users.AnyAsync(u => u.EmployeeCode == request.EmployeeCode && u.Id != id))
                {
                    throw new BusinessRuleValidationException("ORG_DUPLICATE_EMPLOYEE_CODE", "Employee code already exists.");
                }
            }

            user.Update(request.EmployeeCode, request.FullName, request.Email, request.EmploymentStatus, request.AccountStatus);

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The user has been modified by another process.");
            }

            return MapToDto(user);
        });
    }

    public async Task<UserDto?> GetUserByIdAsync(long id)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync()
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var users = await context.Users.AsNoTracking().OrderBy(u => u.FullName).ToListAsync();
        var dtos = users.Select(MapToDto).ToList();
        if (dtos.Count == 0) return dtos;

        // Làm giàu công ty + phòng ban (phân công còn hiệu lực) để hiển thị/lọc trên danh sách.
        var userIds = dtos.Select(d => d.Id).ToList();
        var now = DateTime.UtcNow;

        var companyRows = await context.UserCompanyAssignments.AsNoTracking()
            .Where(a => userIds.Contains(a.UserId) && a.AssignmentStatus == "ACTIVE"
                && a.EffectiveFrom <= now && (a.EffectiveTo == null || a.EffectiveTo > now))
            .Join(context.Companies, a => a.CompanyId, c => c.Id, (a, c) => new { a.UserId, CompanyId = c.Id, c.Name })
            .ToListAsync();

        var deptRows = await context.UserDepartmentAssignments.AsNoTracking()
            .Where(a => userIds.Contains(a.UserId) && a.AssignmentStatus == "ACTIVE"
                && a.EffectiveFrom <= now && (a.EffectiveTo == null || a.EffectiveTo > now))
            .Join(context.Departments, a => a.DepartmentId, d => d.Id, (a, d) => new { a.UserId, DepartmentId = d.Id, d.Name })
            .ToListAsync();

        var compByUser = companyRows.GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => new OrgBriefDto(x.CompanyId, x.Name)).Distinct().ToList());
        var deptByUser = deptRows.GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => new OrgBriefDto(x.DepartmentId, x.Name)).Distinct().ToList());

        foreach (var d in dtos)
        {
            if (compByUser.TryGetValue(d.Id, out var cs)) d.Companies = cs;
            if (deptByUser.TryGetValue(d.Id, out var ds)) d.Departments = ds;
        }
        return dtos;
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            EmployeeCode = user.EmployeeCode,
            FullName = user.FullName,
            Email = user.Email,
            EmploymentStatus = user.EmploymentStatus,
            AccountStatus = user.AccountStatus,
            RowVersion = Convert.ToBase64String(user.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
