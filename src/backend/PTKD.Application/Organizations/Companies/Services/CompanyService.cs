using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Organizations.Companies.DTOs;
using PTKD.Domain.Entities;
using PTKD.Domain.Services;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Organizations.Companies.Services;

public class CompanyService : ICompanyService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CompanyService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            if (await context.Companies.AnyAsync(c => c.CompanyCode == request.CompanyCode))
            {
                throw new BusinessRuleValidationException("ORG_DUPLICATE_COMPANY_CODE", "Company code already exists.");
            }

            if (request.ParentCompanyId.HasValue)
            {
                var parentExists = await context.Companies.AnyAsync(c => c.Id == request.ParentCompanyId.Value);
                if (!parentExists)
                {
                    throw new EntityNotFoundException("ORG_COMPANY_NOT_FOUND", "Parent company not found.");
                }
            }

            var company = new Company(request.CompanyCode, request.ParentCompanyId, request.Name, request.TaxCode);
            
            context.Companies.Add(company);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapToDto(company);
        });
    }

    public async Task<CompanyDto> UpdateCompanyAsync(long id, UpdateCompanyRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;
        
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
            {
                throw new EntityNotFoundException("ORG_COMPANY_NOT_FOUND", "Company not found.");
            }

            if (!company.RowVersion.SequenceEqual(rowVersion))
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The company has been modified by another process.");
            }

            if (company.CompanyCode != request.CompanyCode)
            {
                if (await context.Companies.AnyAsync(c => c.CompanyCode == request.CompanyCode && c.Id != id))
                {
                    throw new BusinessRuleValidationException("ORG_DUPLICATE_COMPANY_CODE", "Company code already exists.");
                }
            }

            if (request.ParentCompanyId.HasValue && request.ParentCompanyId.Value != company.ParentCompanyId)
            {
                var allNodes = await context.Companies
                    .Select(c => new { c.Id, c.ParentCompanyId })
                    .ToDictionaryAsync(c => c.Id, c => c.ParentCompanyId);

                if (HierarchyCycleDetector.HasCycle(id, request.ParentCompanyId, allNodes))
                {
                    throw new BusinessRuleValidationException("ORG_HIERARCHY_CYCLE_DETECTED", "Parent company creates a cycle in the hierarchy.");
                }
            }

            company.Update(request.CompanyCode, request.ParentCompanyId, request.Name, request.TaxCode);

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The company has been modified by another process.");
            }

            return MapToDto(company);
        });
    }

    public async Task<CompanyDto> UpdateCompanyStatusAsync(long id, UpdateCompanyStatusRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.TargetVersion).Value;
        
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = _dbContextFactory.CreateDbContext();
            await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == id);
            if (company == null)
            {
                throw new EntityNotFoundException("ORG_COMPANY_NOT_FOUND", "Company not found.");
            }

            if (!company.RowVersion.SequenceEqual(rowVersion))
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The company has been modified by another process.");
            }

            if (!request.IsActive && company.IsActive)
            {
                // Deactivation rules
                if (await context.Companies.AnyAsync(c => c.ParentCompanyId == id && c.IsActive))
                {
                    throw new BusinessRuleValidationException("ORG_COMPANY_HAS_ACTIVE_DEPENDENCIES", "Cannot deactivate company because it has active child companies.");
                }

                if (await context.Departments.AnyAsync(d => d.CompanyId == id && d.IsActive))
                {
                    throw new BusinessRuleValidationException("ORG_COMPANY_HAS_ACTIVE_DEPENDENCIES", "Cannot deactivate company because it has active departments.");
                }

                if (await context.UserCompanyAssignments.AnyAsync(a => a.CompanyId == id && a.AssignmentStatus == "ACTIVE"))
                {
                    throw new BusinessRuleValidationException("ORG_COMPANY_HAS_ACTIVE_DEPENDENCIES", "Cannot deactivate company because it has active user assignments.");
                }
            }

            company.SetStatus(request.IsActive);

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "The company has been modified by another process.");
            }

            return MapToDto(company);
        });
    }

    public async Task<CompanyDto?> GetCompanyByIdAsync(long id)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var company = await context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return company == null ? null : MapToDto(company);
    }

    public async Task<IEnumerable<CompanyDto>> GetCompaniesAsync()
    {
        await using var context = _dbContextFactory.CreateDbContext();
        var companies = await context.Companies.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        return companies.Select(MapToDto);
    }

    private static CompanyDto MapToDto(Company company)
    {
        return new CompanyDto
        {
            Id = company.Id,
            CompanyCode = company.CompanyCode,
            ParentCompanyId = company.ParentCompanyId,
            Name = company.Name,
            TaxCode = company.TaxCode,
            IsActive = company.IsActive,
            RowVersion = Convert.ToBase64String(company.RowVersion ?? Array.Empty<byte>()),
            CreatedAt = company.CreatedAt,
            UpdatedAt = company.UpdatedAt
        };
    }
}
