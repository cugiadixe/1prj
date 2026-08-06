using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Organizations.Departments.DTOs;
using PTKD.Domain.Entities;
using PTKD.Domain.Services;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Organizations.Departments.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public DepartmentService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            if (await context.Departments.AnyAsync(d => d.DepartmentCode == request.DepartmentCode))
            {
                throw new BusinessRuleValidationException("ORG_DUPLICATE_DEPARTMENT_CODE", "Department code already exists.");
            }

            var companyExists = await context.Companies.AnyAsync(c => c.Id == request.CompanyId && c.IsActive);
            if (!companyExists)
            {
                throw new BusinessRuleValidationException("ORG_COMPANY_NOT_FOUND", "Active company not found.");
            }

            if (request.ParentDepartmentId.HasValue)
            {
                var parent = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.ParentDepartmentId.Value);
                if (parent == null)
                {
                    throw new EntityNotFoundException("ORG_DEPARTMENT_NOT_FOUND", "Parent department not found.");
                }
                if (parent.CompanyId != request.CompanyId)
                {
                    throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Parent department must belong to the same company.");
                }
            }

            var department = new Department(request.DepartmentCode, request.CompanyId, request.ParentDepartmentId, request.Name);
            
            context.Departments.Add(department);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToDto(department);
        });
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(long id, UpdateDepartmentRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;
        
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == id);
            if (department == null)
            {
                throw new EntityNotFoundException("ORG_DEPARTMENT_NOT_FOUND", "Department not found.");
            }

            if (!department.RowVersion.SequenceEqual(rowVersion))
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The department has been modified by another process.");
            }

            if (department.DepartmentCode != request.DepartmentCode)
            {
                if (await context.Departments.AnyAsync(d => d.DepartmentCode == request.DepartmentCode && d.Id != id))
                {
                    throw new BusinessRuleValidationException("ORG_DUPLICATE_DEPARTMENT_CODE", "Department code already exists.");
                }
            }

            if (request.ParentDepartmentId.HasValue && request.ParentDepartmentId.Value != department.ParentDepartmentId)
            {
                var parent = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.ParentDepartmentId.Value);
                if (parent == null)
                {
                    throw new EntityNotFoundException("ORG_DEPARTMENT_NOT_FOUND", "Parent department not found.");
                }
                if (parent.CompanyId != department.CompanyId)
                {
                    throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Parent department must belong to the same company.");
                }

                var allNodes = await context.Departments
                    .Where(d => d.CompanyId == department.CompanyId)
                    .Select(d => new { d.Id, d.ParentDepartmentId })
                    .ToDictionaryAsync(d => d.Id, d => d.ParentDepartmentId);

                if (HierarchyCycleDetector.HasCycle(id, request.ParentDepartmentId, allNodes))
                {
                    throw new BusinessRuleValidationException("ORG_HIERARCHY_CYCLE_DETECTED", "Parent department creates a cycle in the hierarchy.");
                }
            }

            department.Update(request.DepartmentCode, request.ParentDepartmentId, request.Name);

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The department has been modified by another process.");
            }

            return MapToDto(department);
        });
    }

    public async Task<DepartmentDto> UpdateDepartmentStatusAsync(long id, UpdateDepartmentStatusRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;
        
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == id);
            if (department == null)
            {
                throw new EntityNotFoundException("ORG_DEPARTMENT_NOT_FOUND", "Department not found.");
            }

            if (!department.RowVersion.SequenceEqual(rowVersion))
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The department has been modified by another process.");
            }

            if (!request.IsActive && department.IsActive)
            {
                // Deactivation rules
                if (await context.Departments.AnyAsync(d => d.ParentDepartmentId == id && d.IsActive))
                {
                    throw new BusinessRuleValidationException("ORG_DEPARTMENT_HAS_ACTIVE_DEPENDENCIES", "Cannot deactivate department because it has active child departments.");
                }

                if (await context.UserDepartmentAssignments.AnyAsync(a => a.DepartmentId == id && a.AssignmentStatus == "ACTIVE"))
                {
                    throw new BusinessRuleValidationException("ORG_DEPARTMENT_HAS_ACTIVE_DEPENDENCIES", "Cannot deactivate department because it has active user assignments.");
                }
                
                // Also need to check if it's primary anywhere. It would be active above, but let's be explicit if needed.
                if (await context.UserDepartmentAssignments.AnyAsync(a => a.DepartmentId == id && a.AssignmentStatus == "ACTIVE" && a.IsPrimaryForCompany))
                {
                    throw new BusinessRuleValidationException("ORG_DEPARTMENT_IS_PRIMARY", "Cannot deactivate a primary department.");
                }
            }

            department.SetStatus(request.IsActive);

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The department has been modified by another process.");
            }

            return MapToDto(department);
        });
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(long id)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var department = await context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        return department == null ? null : MapToDto(department);
    }

    public async Task<IEnumerable<DepartmentDto>> GetDepartmentsAsync(long companyId)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var departments = await context.Departments.AsNoTracking()
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.Name)
            .ToListAsync();
        return departments.Select(MapToDto);
    }

    private static DepartmentDto MapToDto(Department department)
    {
        return new DepartmentDto
        {
            Id = department.Id,
            DepartmentCode = department.DepartmentCode,
            CompanyId = department.CompanyId,
            ParentDepartmentId = department.ParentDepartmentId,
            Name = department.Name,
            IsActive = department.IsActive,
            RowVersion = Convert.ToBase64String(department.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt
        };
    }
}
