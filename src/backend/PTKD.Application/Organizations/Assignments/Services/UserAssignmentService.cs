using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Organizations.Assignments.DTOs;
using PTKD.Domain.Entities;
using PTKD.Domain.ValueObjects;

namespace PTKD.Application.Organizations.Assignments.Services;

public class UserAssignmentService : IUserAssignmentService
{
    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public UserAssignmentService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    private async Task ExecuteWithRetryAsync(Func<IOrganizationDbContext, Task> operation)
    {
        await using var tempContext = _dbContextFactory.CreateDbContext();
        var strategy = tempContext.CreateExecutionStrategy();
        
        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var context = _dbContextFactory.CreateDbContext();
                await using var transaction = await context.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                
                await operation(context);
                
                await transaction.CommitAsync();
            });
        }
        catch (Microsoft.EntityFrameworkCore.Storage.RetryLimitExceededException)
        {
            throw new BusinessRuleValidationException("ORG_TRANSACTION_RETRY_EXHAUSTED", "The operation could not be completed after maximum retries due to deadlocks.");
        }
    }

    public async Task AssignCompanyAsync(long userId, AssignCompanyRequest request)
    {
        await ExecuteWithRetryAsync(async context =>
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) throw new EntityNotFoundException("ORG_USER_NOT_FOUND", "User not found.");

            var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId && c.IsActive);
            if (company == null) throw new BusinessRuleValidationException("ORG_INACTIVE_COMPANY", "Company not found or inactive.");

            var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.PrimaryDepartmentId && d.IsActive && d.CompanyId == request.CompanyId);
            if (department == null) throw new BusinessRuleValidationException("ORG_DEPARTMENT_COMPANY_MISMATCH", "Primary department not found, inactive, or belongs to another company.");

            var existingActive = await context.UserCompanyAssignments.AnyAsync(a => a.UserId == userId && a.CompanyId == request.CompanyId && a.AssignmentStatus == "ACTIVE");
            if (existingActive) throw new BusinessRuleValidationException("ORG_COMPANY_ASSIGNMENT_ALREADY_ACTIVE", "Active assignment for this company already exists.");

            var requestedTimeline = AssignmentTimeline.Create(request.EffectiveFrom, null);
            var historicalOverlap = await context.UserCompanyAssignments
                .Where(a => a.UserId == userId && a.CompanyId == request.CompanyId)
                .ToListAsync();

            foreach (var hist in historicalOverlap)
            {
                var histTimeline = hist.GetTimeline();
                if (histTimeline.Overlaps(requestedTimeline))
                {
                    throw new BusinessRuleValidationException("ORG_TEMPORAL_OVERLAP", "New assignment timeline overlaps with a historical assignment.");
                }
            }

            var hasPrimary = await context.UserCompanyAssignments.AnyAsync(a => a.UserId == userId && a.AssignmentStatus == "ACTIVE" && a.IsPrimary);
            bool isPrimary = !hasPrimary;

            var companyAssignment = new UserCompanyAssignment(userId, company.Id, isPrimary, request.EffectiveFrom);
            context.UserCompanyAssignments.Add(companyAssignment);
            await context.SaveChangesAsync();

            var departmentAssignment = new UserDepartmentAssignment(userId, department.Id, companyAssignment.Id, company.Id, true, request.EffectiveFrom);
            context.UserDepartmentAssignments.Add(departmentAssignment);
            await context.SaveChangesAsync();

            var history = new EmploymentHistory(
                userId: userId,
                actionType: "ASSIGNED_COMPANY",
                effectiveDate: request.EffectiveFrom,
                reason: request.Reason,
                toCompanyId: company.Id,
                toDepartmentId: department.Id,
                toCompanyAssignmentId: companyAssignment.Id,
                toDepartmentAssignmentId: departmentAssignment.Id
            );
            context.EmploymentHistories.Add(history);
            await context.SaveChangesAsync();
        });
    }

    public async Task AssignDepartmentAsync(long userId, AssignDepartmentRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.CompanyAssignmentRowVersion).Value;

        await ExecuteWithRetryAsync(async context =>
        {
            var companyAssignment = await context.UserCompanyAssignments
                .FirstOrDefaultAsync(a => a.Id == request.UserCompanyAssignmentId && a.UserId == userId);
            
            if (companyAssignment == null) throw new EntityNotFoundException("ORG_COMPANY_ASSIGNMENT_REQUIRED", "Company assignment not found for this user.");
            
            if (!companyAssignment.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Company assignment has been modified.");
                
            if (companyAssignment.AssignmentStatus != "ACTIVE")
                throw new BusinessRuleValidationException("ORG_COMPANY_ASSIGNMENT_CLOSED", "Company assignment is closed.");

            var department = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.DepartmentId && d.IsActive && d.CompanyId == companyAssignment.CompanyId);
            if (department == null) throw new BusinessRuleValidationException("ORG_DEPARTMENT_COMPANY_MISMATCH", "Department not found, inactive, or belongs to another company.");

            var existingActive = await context.UserDepartmentAssignments.AnyAsync(a => a.UserId == userId && a.DepartmentId == request.DepartmentId && a.AssignmentStatus == "ACTIVE");
            if (existingActive) throw new BusinessRuleValidationException("ORG_DEPARTMENT_ASSIGNMENT_ALREADY_ACTIVE", "Active assignment for this department already exists.");

            var requestedTimeline = AssignmentTimeline.Create(request.EffectiveFrom, null);
            var historicalOverlap = await context.UserDepartmentAssignments
                .Where(a => a.UserId == userId && a.DepartmentId == request.DepartmentId)
                .ToListAsync();

            foreach (var hist in historicalOverlap)
            {
                var histTimeline = hist.GetTimeline();
                if (histTimeline.Overlaps(requestedTimeline))
                {
                    throw new BusinessRuleValidationException("ORG_TEMPORAL_OVERLAP", "New assignment timeline overlaps with a historical assignment.");
                }
            }

            var departmentAssignment = new UserDepartmentAssignment(userId, department.Id, companyAssignment.Id, companyAssignment.CompanyId, false, request.EffectiveFrom);
            context.UserDepartmentAssignments.Add(departmentAssignment);
            await context.SaveChangesAsync();

            var history = new EmploymentHistory(
                userId: userId,
                actionType: "ASSIGNED_DEPARTMENT",
                effectiveDate: request.EffectiveFrom,
                reason: request.Reason,
                toCompanyId: companyAssignment.CompanyId,
                toDepartmentId: department.Id,
                toCompanyAssignmentId: companyAssignment.Id,
                toDepartmentAssignmentId: departmentAssignment.Id
            );
            context.EmploymentHistories.Add(history);
            await context.SaveChangesAsync();
        });
    }

    public async Task ChangePrimaryCompanyAsync(long userId, long companyAssignmentId, ChangePrimaryCompanyRequest request)
    {
        var targetRowVersion = RowVersion.FromBase64(request.TargetRowVersion).Value;
        var currentPrimaryRowVersion = RowVersion.FromBase64(request.CurrentPrimaryRowVersion).Value;

        await ExecuteWithRetryAsync(async context =>
        {
            var targetAssignment = await context.UserCompanyAssignments.FirstOrDefaultAsync(a => a.Id == companyAssignmentId && a.UserId == userId);
            if (targetAssignment == null) throw new EntityNotFoundException("ORG_COMPANY_ASSIGNMENT_REQUIRED", "Target assignment not found.");
            
            if (targetAssignment.AssignmentStatus != "ACTIVE") throw new BusinessRuleValidationException("ORG_INACTIVE_COMPANY", "Target assignment is inactive.");
            if (targetAssignment.IsPrimary) throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Target assignment is already primary.");
            
            var currentPrimary = await context.UserCompanyAssignments.FirstOrDefaultAsync(a => a.Id == request.CurrentPrimaryAssignmentId && a.UserId == userId);
            if (currentPrimary == null) throw new EntityNotFoundException("ORG_COMPANY_ASSIGNMENT_REQUIRED", "Current primary assignment not found.");
            if (!currentPrimary.IsPrimary || currentPrimary.AssignmentStatus != "ACTIVE") throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Current primary assignment is invalid.");

            if (!targetAssignment.RowVersion.SequenceEqual(targetRowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Target assignment has been modified.");
                
            if (!currentPrimary.RowVersion.SequenceEqual(currentPrimaryRowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Current primary assignment has been modified.");

            // Safe ordering
            currentPrimary.SetPrimary(false);
            await context.SaveChangesAsync();
            
            targetAssignment.SetPrimary(true);
            await context.SaveChangesAsync();

            // Validate exactly one primary
            var activePrimaries = await context.UserCompanyAssignments.CountAsync(a => a.UserId == userId && a.AssignmentStatus == "ACTIVE" && a.IsPrimary);
            if (activePrimaries != 1) throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Data integrity violation: exactly one primary company is required.");

            var history = new EmploymentHistory(
                userId: userId,
                actionType: "CHANGED_PRIMARY_COMPANY",
                effectiveDate: DateTime.UtcNow,
                reason: request.Reason,
                fromCompanyId: currentPrimary.CompanyId,
                toCompanyId: targetAssignment.CompanyId,
                fromCompanyAssignmentId: currentPrimary.Id,
                toCompanyAssignmentId: targetAssignment.Id
            );
            context.EmploymentHistories.Add(history);
            await context.SaveChangesAsync();
        });
    }

    public async Task ChangePrimaryDepartmentAsync(long userId, long departmentAssignmentId, ChangePrimaryDepartmentRequest request)
    {
        var targetRowVersion = RowVersion.FromBase64(request.TargetRowVersion).Value;
        var currentPrimaryRowVersion = RowVersion.FromBase64(request.CurrentPrimaryRowVersion).Value;

        await ExecuteWithRetryAsync(async context =>
        {
            var targetAssignment = await context.UserDepartmentAssignments.FirstOrDefaultAsync(a => a.Id == departmentAssignmentId && a.UserId == userId);
            if (targetAssignment == null) throw new EntityNotFoundException("ORG_DEPARTMENT_ASSIGNMENT_REQUIRED", "Target assignment not found.");
            
            if (targetAssignment.AssignmentStatus != "ACTIVE") throw new BusinessRuleValidationException("ORG_INACTIVE_DEPARTMENT", "Target assignment is inactive.");
            if (targetAssignment.IsPrimaryForCompany) throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Target assignment is already primary.");
            
            var currentPrimary = await context.UserDepartmentAssignments.FirstOrDefaultAsync(a => a.Id == request.CurrentPrimaryAssignmentId && a.UserId == userId);
            if (currentPrimary == null) throw new EntityNotFoundException("ORG_DEPARTMENT_ASSIGNMENT_REQUIRED", "Current primary assignment not found.");
            if (!currentPrimary.IsPrimaryForCompany || currentPrimary.AssignmentStatus != "ACTIVE") throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Current primary assignment is invalid.");

            if (targetAssignment.CompanyId != currentPrimary.CompanyId || targetAssignment.UserCompanyAssignmentId != currentPrimary.UserCompanyAssignmentId)
                throw new BusinessRuleValidationException("ORG_DEPARTMENT_COMPANY_MISMATCH", "Assignments do not belong to the same company assignment.");

            if (!targetAssignment.RowVersion.SequenceEqual(targetRowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Target assignment has been modified.");
                
            if (!currentPrimary.RowVersion.SequenceEqual(currentPrimaryRowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Current primary assignment has been modified.");

            // Safe ordering
            currentPrimary.SetPrimary(false);
            await context.SaveChangesAsync();
            
            targetAssignment.SetPrimary(true);
            await context.SaveChangesAsync();

            var activePrimaries = await context.UserDepartmentAssignments.CountAsync(a => a.UserCompanyAssignmentId == targetAssignment.UserCompanyAssignmentId && a.AssignmentStatus == "ACTIVE" && a.IsPrimaryForCompany);
            if (activePrimaries != 1) throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Exactly one primary department is required for the company assignment.");

            var history = new EmploymentHistory(
                userId: userId,
                actionType: "CHANGED_PRIMARY_DEPARTMENT",
                effectiveDate: DateTime.UtcNow,
                reason: request.Reason,
                fromDepartmentId: currentPrimary.DepartmentId,
                toDepartmentId: targetAssignment.DepartmentId,
                fromDepartmentAssignmentId: currentPrimary.Id,
                toDepartmentAssignmentId: targetAssignment.Id
            );
            context.EmploymentHistories.Add(history);
            await context.SaveChangesAsync();
        });
    }

    public async Task CloseCompanyAssignmentAsync(long userId, long companyAssignmentId, CloseCompanyAssignmentRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.CompanyAssignmentRowVersion).Value;

        await ExecuteWithRetryAsync(async context =>
        {
            var assignment = await context.UserCompanyAssignments.FirstOrDefaultAsync(a => a.Id == companyAssignmentId && a.UserId == userId);
            if (assignment == null) throw new EntityNotFoundException("ORG_COMPANY_ASSIGNMENT_REQUIRED", "Assignment not found.");
            
            if (!assignment.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Assignment has been modified.");
                
            if (assignment.AssignmentStatus != "ACTIVE")
                throw new BusinessRuleValidationException("ORG_COMPANY_ASSIGNMENT_CLOSED", "Assignment is already closed.");

            var activeCount = await context.UserCompanyAssignments.CountAsync(a => a.UserId == userId && a.AssignmentStatus == "ACTIVE");
            if (activeCount == 1)
                throw new BusinessRuleValidationException("ORG_USER_REQUIRES_ACTIVE_COMPANY", "Cannot close the final active company assignment.");

            if (assignment.IsPrimary)
            {
                if (!request.ReplacementPrimaryCompanyAssignmentId.HasValue || string.IsNullOrEmpty(request.ReplacementPrimaryCompanyRowVersion))
                    throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement primary company fields are required.");

                var repRowVersion = RowVersion.FromBase64(request.ReplacementPrimaryCompanyRowVersion).Value;
                var replacement = await context.UserCompanyAssignments.FirstOrDefaultAsync(a => a.Id == request.ReplacementPrimaryCompanyAssignmentId.Value && a.UserId == userId);
                if (replacement == null || replacement.AssignmentStatus != "ACTIVE")
                    throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement assignment must be active.");
                    
                if (!replacement.RowVersion.SequenceEqual(repRowVersion))
                    throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Replacement assignment has been modified.");
                if (replacement.Id == assignment.Id)
                    throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement cannot be the same assignment.");

                // Safe ordering
                assignment.SetPrimary(false);
                await context.SaveChangesAsync();
                
                replacement.SetPrimary(true);
                await context.SaveChangesAsync();
            }
            else
            {
                if (request.ReplacementPrimaryCompanyAssignmentId.HasValue || !string.IsNullOrEmpty(request.ReplacementPrimaryCompanyRowVersion))
                    throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement primary fields must be null when closing non-primary.");
            }

            var activeDepartments = await context.UserDepartmentAssignments
                .Where(a => a.UserCompanyAssignmentId == assignment.Id && a.AssignmentStatus == "ACTIVE")
                .OrderBy(a => a.Id) // deterministic ID order
                .ToListAsync();

            try
            {
                foreach (var dept in activeDepartments)
                {
                    dept.Close(request.EffectiveTo);
                }
                assignment.Close(request.EffectiveTo);
                
                var history = new EmploymentHistory(
                    userId: userId,
                    actionType: "CLOSED_COMPANY",
                    effectiveDate: request.EffectiveTo,
                    reason: request.Reason,
                    fromCompanyId: assignment.CompanyId,
                    fromCompanyAssignmentId: assignment.Id
                );
                context.EmploymentHistories.Add(history);

                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "A dependent assignment has been modified by another process.");
            }
        });
    }

    public async Task SameCompanyDepartmentTransferAsync(long userId, long companyAssignmentId, SameCompanyDepartmentTransferRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.CompanyAssignmentRowVersion).Value;
        var sourceDeptRowVersion = RowVersion.FromBase64(request.SourceDepartmentAssignmentRowVersion).Value;

        await ExecuteWithRetryAsync(async context =>
        {
            var companyAssignment = await context.UserCompanyAssignments.FirstOrDefaultAsync(a => a.Id == companyAssignmentId && a.UserId == userId);
            if (companyAssignment == null) throw new EntityNotFoundException("ORG_COMPANY_ASSIGNMENT_REQUIRED", "Assignment not found.");
            
            if (!companyAssignment.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Company assignment has been modified.");
                
            if (companyAssignment.AssignmentStatus != "ACTIVE")
                throw new BusinessRuleValidationException("ORG_COMPANY_ASSIGNMENT_CLOSED", "Company assignment is closed.");

            var sourceDept = await context.UserDepartmentAssignments.FirstOrDefaultAsync(a => a.Id == request.SourceDepartmentAssignmentId && a.UserId == userId);
            if (sourceDept == null || sourceDept.AssignmentStatus != "ACTIVE")
                throw new EntityNotFoundException("ORG_DEPARTMENT_ASSIGNMENT_REQUIRED", "Source department assignment not found or inactive.");
                
            if (!sourceDept.RowVersion.SequenceEqual(sourceDeptRowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Source department assignment has been modified.");

            if (sourceDept.UserCompanyAssignmentId != companyAssignmentId)
                throw new BusinessRuleValidationException("ORG_DEPARTMENT_COMPANY_MISMATCH", "Source department does not belong to the company assignment.");

            if (request.TargetDepartmentId == sourceDept.DepartmentId)
                throw new BusinessRuleValidationException("ORG_SOURCE_TARGET_DEPARTMENT_SAME", "Source and target departments cannot be the same.");

            var targetDept = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.TargetDepartmentId && d.IsActive && d.CompanyId == companyAssignment.CompanyId);
            if (targetDept == null) throw new BusinessRuleValidationException("ORG_DEPARTMENT_COMPANY_MISMATCH", "Target department not found, inactive, or belongs to another company.");

            if (request.EffectiveDate <= sourceDept.EffectiveFrom)
                throw new BusinessRuleValidationException("ORG_INVALID_EFFECTIVE_DATE", "Effective date must be strictly greater than source assignment EffectiveFrom.");

            var existingActive = await context.UserDepartmentAssignments.AnyAsync(a => a.UserId == userId && a.DepartmentId == request.TargetDepartmentId && a.AssignmentStatus == "ACTIVE");
            if (existingActive) throw new BusinessRuleValidationException("ORG_DEPARTMENT_ASSIGNMENT_ALREADY_ACTIVE", "Active assignment for target department already exists.");

            var requestedTimeline = AssignmentTimeline.Create(request.EffectiveDate, null);
            var historicalOverlap = await context.UserDepartmentAssignments
                .Where(a => a.UserId == userId && a.DepartmentId == request.TargetDepartmentId)
                .ToListAsync();

            foreach (var hist in historicalOverlap)
            {
                if (hist.GetTimeline().Overlaps(requestedTimeline))
                    throw new BusinessRuleValidationException("ORG_TEMPORAL_OVERLAP", "Target assignment timeline overlaps with a historical assignment.");
            }

            bool isPrimary = sourceDept.IsPrimaryForCompany;
            
            if (isPrimary)
            {
                sourceDept.SetPrimary(false);
                await context.SaveChangesAsync();
            }

            sourceDept.Close(request.EffectiveDate);
            
            var targetAssignment = new UserDepartmentAssignment(userId, targetDept.Id, companyAssignmentId, companyAssignment.CompanyId, isPrimary, request.EffectiveDate);
            context.UserDepartmentAssignments.Add(targetAssignment);

            var history = new EmploymentHistory(
                userId: userId,
                actionType: "TRANSFERRED_DEPARTMENT_SAME_COMPANY",
                effectiveDate: request.EffectiveDate,
                reason: request.Reason,
                fromDepartmentId: sourceDept.DepartmentId,
                toDepartmentId: targetDept.Id,
                fromDepartmentAssignmentId: sourceDept.Id,
                toDepartmentAssignmentId: 0 // Will get identity upon save, but we can't capture it here easily unless we save first. 
                // Let's save the assignment to get its ID.
            );
            
            await context.SaveChangesAsync(); // gets identity for targetAssignment
            
            history = new EmploymentHistory(
                userId: userId,
                actionType: "TRANSFERRED_DEPARTMENT_SAME_COMPANY",
                effectiveDate: request.EffectiveDate,
                reason: request.Reason,
                fromDepartmentId: sourceDept.DepartmentId,
                toDepartmentId: targetDept.Id,
                fromDepartmentAssignmentId: sourceDept.Id,
                toDepartmentAssignmentId: targetAssignment.Id
            );
            context.EmploymentHistories.Add(history);
            await context.SaveChangesAsync();
        });
    }

    public async Task CrossCompanyTransferAsync(long userId, long sourceCompanyAssignmentId, CrossCompanyTransferRequest request)
    {
        var rowVersion = RowVersion.FromBase64(request.SourceCompanyAssignmentRowVersion).Value;

        await ExecuteWithRetryAsync(async context =>
        {
            var sourceAssignment = await context.UserCompanyAssignments.FirstOrDefaultAsync(a => a.Id == sourceCompanyAssignmentId && a.UserId == userId);
            if (sourceAssignment == null) throw new EntityNotFoundException("ORG_COMPANY_ASSIGNMENT_REQUIRED", "Source assignment not found.");
            
            if (!sourceAssignment.RowVersion.SequenceEqual(rowVersion))
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Source assignment has been modified.");
                
            if (sourceAssignment.AssignmentStatus != "ACTIVE")
                throw new BusinessRuleValidationException("ORG_COMPANY_ASSIGNMENT_CLOSED", "Source assignment is already closed.");

            if (request.TargetCompanyId == sourceAssignment.CompanyId)
                throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Target company cannot be the same as source company.");

            var targetCompany = await context.Companies.FirstOrDefaultAsync(c => c.Id == request.TargetCompanyId && c.IsActive);
            if (targetCompany == null) throw new BusinessRuleValidationException("ORG_INACTIVE_COMPANY", "Target company not found or inactive.");

            var targetDepartment = await context.Departments.FirstOrDefaultAsync(d => d.Id == request.TargetDepartmentId && d.IsActive && d.CompanyId == request.TargetCompanyId);
            if (targetDepartment == null) throw new BusinessRuleValidationException("ORG_INACTIVE_DEPARTMENT", "Target department not found, inactive, or belongs to another company.");

            var existingTargetComp = await context.UserCompanyAssignments.AnyAsync(a => a.UserId == userId && a.CompanyId == request.TargetCompanyId && a.AssignmentStatus == "ACTIVE");
            if (existingTargetComp) throw new BusinessRuleValidationException("ORG_COMPANY_ASSIGNMENT_ALREADY_ACTIVE", "Active assignment for target company already exists.");

            var requestedTimeline = AssignmentTimeline.Create(request.EffectiveDate, null);
            var historicalCompOverlap = await context.UserCompanyAssignments.Where(a => a.UserId == userId && a.CompanyId == request.TargetCompanyId).ToListAsync();
            foreach (var hist in historicalCompOverlap)
            {
                if (hist.GetTimeline().Overlaps(requestedTimeline)) throw new BusinessRuleValidationException("ORG_TEMPORAL_OVERLAP", "Target company assignment overlaps with history.");
            }

            var existingTargetDept = await context.UserDepartmentAssignments.AnyAsync(a => a.UserId == userId && a.DepartmentId == request.TargetDepartmentId && a.AssignmentStatus == "ACTIVE");
            if (existingTargetDept) throw new BusinessRuleValidationException("ORG_DEPARTMENT_ASSIGNMENT_ALREADY_ACTIVE", "Active assignment for target department already exists.");

            if (sourceAssignment.IsPrimary)
            {
                if (request.MakeTargetPrimaryCompany)
                {
                    if (request.ReplacementPrimaryCompanyAssignmentId.HasValue || !string.IsNullOrEmpty(request.ReplacementPrimaryCompanyRowVersion))
                        throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement fields must be null when making target primary.");
                }
                else
                {
                    if (!request.ReplacementPrimaryCompanyAssignmentId.HasValue || string.IsNullOrEmpty(request.ReplacementPrimaryCompanyRowVersion))
                        throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement fields are required when source is primary and target is not primary.");
                    
                    var repRowVersion = RowVersion.FromBase64(request.ReplacementPrimaryCompanyRowVersion).Value;
                    var replacement = await context.UserCompanyAssignments.FirstOrDefaultAsync(a => a.Id == request.ReplacementPrimaryCompanyAssignmentId.Value && a.UserId == userId);
                    if (replacement == null || replacement.AssignmentStatus != "ACTIVE")
                        throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement assignment must be active.");
                        
                    if (!replacement.RowVersion.SequenceEqual(repRowVersion))
                        throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "Replacement assignment has been modified.");
                        
                    if (replacement.Id == sourceAssignment.Id)
                        throw new BusinessRuleValidationException("ORG_VALIDATION_FAILED", "Replacement cannot be the same assignment.");

                    sourceAssignment.SetPrimary(false);
                    await context.SaveChangesAsync();
                    
                    replacement.SetPrimary(true);
                    await context.SaveChangesAsync();
                }
            }
            else
            {
                if (request.MakeTargetPrimaryCompany)
                    throw new BusinessRuleValidationException("ORG_INVALID_PRIMARY_TRANSFER_REQUEST", "Target cannot be made primary if source is not primary.");
                    
                if (request.ReplacementPrimaryCompanyAssignmentId.HasValue || !string.IsNullOrEmpty(request.ReplacementPrimaryCompanyRowVersion))
                    throw new BusinessRuleValidationException("ORG_INVALID_PRIMARY_TRANSFER_REQUEST", "Replacement fields must be null when source is not primary.");
            }

            try
            {
                if (sourceAssignment.IsPrimary && request.MakeTargetPrimaryCompany)
                {
                    sourceAssignment.SetPrimary(false);
                    await context.SaveChangesAsync();
                }
                
                var newCompanyAssignment = new UserCompanyAssignment(userId, targetCompany.Id, request.MakeTargetPrimaryCompany, request.EffectiveDate);
                context.UserCompanyAssignments.Add(newCompanyAssignment);
                await context.SaveChangesAsync();

                var activeDepartments = await context.UserDepartmentAssignments
                    .Where(a => a.UserCompanyAssignmentId == sourceAssignment.Id && a.AssignmentStatus == "ACTIVE")
                    .OrderBy(a => a.Id)
                    .ToListAsync();
                
                foreach (var dept in activeDepartments)
                {
                    dept.Close(request.EffectiveDate);
                }
                
                sourceAssignment.Close(request.EffectiveDate);

                var newDeptAssignment = new UserDepartmentAssignment(userId, targetDepartment.Id, newCompanyAssignment.Id, targetCompany.Id, true, request.EffectiveDate);
                context.UserDepartmentAssignments.Add(newDeptAssignment);
                await context.SaveChangesAsync();

                var history = new EmploymentHistory(
                    userId: userId,
                    actionType: "TRANSFERRED_CROSS_COMPANY",
                    effectiveDate: request.EffectiveDate,
                    reason: request.Reason,
                    fromCompanyId: sourceAssignment.CompanyId,
                    toCompanyId: targetCompany.Id,
                    fromCompanyAssignmentId: sourceAssignment.Id,
                    toCompanyAssignmentId: newCompanyAssignment.Id,
                    toDepartmentId: targetDepartment.Id,
                    toDepartmentAssignmentId: newDeptAssignment.Id
                );
                context.EmploymentHistories.Add(history);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConcurrencyException("ORG_INVALID_ROW_VERSION", "A dependent assignment has been modified by another process.");
            }
        });
    }
}
