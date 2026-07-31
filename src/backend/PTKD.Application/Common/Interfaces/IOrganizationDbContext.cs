using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PTKD.Domain.Entities;

namespace PTKD.Application.Common.Interfaces;

public interface IOrganizationDbContext : IDisposable, IAsyncDisposable
{
    DbSet<Company> Companies { get; }
    DbSet<Department> Departments { get; }
    DbSet<User> Users { get; }
    DbSet<UserCompanyAssignment> UserCompanyAssignments { get; }
    DbSet<UserDepartmentAssignment> UserDepartmentAssignments { get; }
    DbSet<EmploymentHistory> EmploymentHistories { get; }
    DbSet<Profile> Profiles { get; }
    DbSet<Customer> Customers { get; }
    DbSet<CustomerCompanyContext> CustomerCompanyContexts { get; }
    DbSet<BusinessProcessCatalog> BusinessProcessCatalogs { get; }
    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<WorkflowDefinitionVersion> WorkflowDefinitionVersions { get; }
    DbSet<WorkflowStep> WorkflowSteps { get; }
    DbSet<WorkflowStepApproverRule> WorkflowStepApproverRules { get; }
    DbSet<WorkflowCondition> WorkflowConditions { get; }
    DbSet<WorkflowBinding> WorkflowBindings { get; }
    DbSet<WorkflowInstance> WorkflowInstances { get; }
    DbSet<WorkflowInstanceStep> WorkflowInstanceSteps { get; }
    DbSet<WorkflowInstanceStepAssignee> WorkflowInstanceStepAssignees { get; }
    DbSet<WorkflowAction> WorkflowActions { get; }

    System.Data.Common.DbConnection GetDbConnection();
    System.Data.Common.DbTransaction? GetCurrentDbTransaction();
    void ClearChangeTracker();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
    IExecutionStrategy CreateExecutionStrategy();
}
