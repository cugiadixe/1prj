using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;
using PTKD.Domain.Security.Authentication;

namespace PTKD.Infrastructure.Persistence;

public class AppDbContext : DbContext, IOrganizationDbContext, IAuthenticationDbContext, ITokenSessionDbContext, PTKD.Application.Security.Authorization.Interfaces.IAuthorizationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserCompanyAssignment> UserCompanyAssignments => Set<UserCompanyAssignment>();
    public DbSet<UserDepartmentAssignment> UserDepartmentAssignments => Set<UserDepartmentAssignment>();
    public DbSet<EmploymentHistory> EmploymentHistories => Set<EmploymentHistory>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerCompanyContext> CustomerCompanyContexts => Set<CustomerCompanyContext>();
    public DbSet<BusinessProcessCatalog> BusinessProcessCatalogs => Set<BusinessProcessCatalog>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowDefinitionVersion> WorkflowDefinitionVersions => Set<WorkflowDefinitionVersion>();
    public DbSet<WorkflowStep> WorkflowSteps => Set<WorkflowStep>();
    public DbSet<WorkflowStepApproverRule> WorkflowStepApproverRules => Set<WorkflowStepApproverRule>();
    public DbSet<WorkflowCondition> WorkflowConditions => Set<WorkflowCondition>();
    public DbSet<WorkflowBinding> WorkflowBindings => Set<WorkflowBinding>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowInstanceStep> WorkflowInstanceSteps => Set<WorkflowInstanceStep>();
    public DbSet<WorkflowInstanceStepAssignee> WorkflowInstanceStepAssignees => Set<WorkflowInstanceStepAssignee>();
    public DbSet<WorkflowAction> WorkflowActions => Set<WorkflowAction>();
    public DbSet<CustomerChangeRequest> CustomerChangeRequests => Set<CustomerChangeRequest>();
    public DbSet<CustomerMergeRequest> CustomerMergeRequests => Set<CustomerMergeRequest>();
    public DbSet<CustomerMergeRequestCandidate> CustomerMergeRequestCandidates => Set<CustomerMergeRequestCandidate>();
    public DbSet<CustomerMergeHistory> CustomerMergeHistory => Set<CustomerMergeHistory>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<ServicePriceHistory> ServicePriceHistories => Set<ServicePriceHistory>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceHistory> ServiceHistories => Set<ServiceHistory>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<PaymentTransactionItem> PaymentTransactionItems => Set<PaymentTransactionItem>();
    public DbSet<PaymentCorrectionHistory> PaymentCorrectionHistories => Set<PaymentCorrectionHistory>();
    public DbSet<ReconciliationPeriod> ReconciliationPeriods => Set<ReconciliationPeriod>();
    public DbSet<UserAuthAccount> UserAuthAccounts => Set<UserAuthAccount>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardReprintRequest> CardReprintRequests => Set<CardReprintRequest>();
    public DbSet<CarePackageRequest> CarePackageRequests => Set<CarePackageRequest>();
    public DbSet<CarePackageRequestItem> CarePackageRequestItems => Set<CarePackageRequestItem>();
    public DbSet<PTKD.Domain.Security.Authorization.Permission> Permissions => Set<PTKD.Domain.Security.Authorization.Permission>();
    public DbSet<PTKD.Domain.Security.Authorization.Role> Roles => Set<PTKD.Domain.Security.Authorization.Role>();
    public DbSet<PTKD.Domain.Security.Authorization.AdminGroup> AdminGroups => Set<PTKD.Domain.Security.Authorization.AdminGroup>();
    public DbSet<PTKD.Domain.Security.Authorization.RolePermission> RolePermissions => Set<PTKD.Domain.Security.Authorization.RolePermission>();
    public DbSet<PTKD.Domain.Security.Authorization.AdminGroupPermission> AdminGroupPermissions => Set<PTKD.Domain.Security.Authorization.AdminGroupPermission>();
    public DbSet<PTKD.Domain.Security.Authorization.DepartmentPermission> DepartmentPermissions => Set<PTKD.Domain.Security.Authorization.DepartmentPermission>();
    public DbSet<PTKD.Domain.Security.Authorization.UserRoleAssignment> UserRoleAssignments => Set<PTKD.Domain.Security.Authorization.UserRoleAssignment>();
    public DbSet<PTKD.Domain.Security.Authorization.UserAdminGroupAssignment> UserAdminGroupAssignments => Set<PTKD.Domain.Security.Authorization.UserAdminGroupAssignment>();
    public DbSet<PTKD.Domain.Security.Authorization.UserIndividualPermission> UserIndividualPermissions => Set<PTKD.Domain.Security.Authorization.UserIndividualPermission>();
    public DbSet<PTKD.Domain.Security.Authorization.AuthorizationPolicyState> AuthorizationPolicyStates => Set<PTKD.Domain.Security.Authorization.AuthorizationPolicyState>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, System.Threading.CancellationToken cancellationToken = default)
    {
        return Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy CreateExecutionStrategy()
    {
        return Database.CreateExecutionStrategy();
    }

    public void ClearChangeTracker()
    {
        ChangeTracker.Clear();
    }

    public System.Data.Common.DbConnection GetDbConnection()
        => Database.GetDbConnection();

    public System.Data.Common.DbTransaction? GetCurrentDbTransaction()
        => Database.CurrentTransaction?.GetDbTransaction();

    public Task<UserAuthAccount?> FindAccountByProviderForUpdateAsync(
        string providerType,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        return UserAuthAccounts
            .FromSqlInterpolated($"SELECT * FROM dbo.User_Auth_Accounts WITH (UPDLOCK, HOLDLOCK) WHERE provider_type = {providerType} AND provider_subject = {providerSubject}")
            .Include(account => account.User)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<UserAuthAccount?> FindAccountByIdForUpdateAsync(
        long accountId,
        CancellationToken cancellationToken = default)
    {
        return UserAuthAccounts
            .FromSqlInterpolated($"SELECT * FROM dbo.User_Auth_Accounts WITH (UPDLOCK, HOLDLOCK) WHERE id = {accountId}")
            .Include(account => account.User)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PasswordHistory>> GetRecentPasswordHistoryAsync(
        long accountId,
        int count,
        CancellationToken cancellationToken = default)
    {
        return await PasswordHistories
            .AsNoTracking()
            .Where(history => history.AccountId == accountId)
            .OrderByDescending(history => history.CreatedAt)
            .ThenByDescending(history => history.Id)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public Task<RefreshToken?> FindRefreshTokenByHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return RefreshTokens
            .FromSqlInterpolated($"SELECT * FROM dbo.Refresh_Tokens WITH (UPDLOCK, HOLDLOCK) WHERE token_hash = {tokenHash}")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> RevokeFamilyAsync(
        Guid familyId,
        string reason,
        DateTime revokedAt,
        CancellationToken cancellationToken = default)
    {
        return await RefreshTokens
            .Where(r => r.FamilyId == familyId && r.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(b => b.RevokedAt, revokedAt)
                    .SetProperty(b => b.RevokeReason, reason),
                cancellationToken);
    }

    public async Task MarkReuseDetectedAsync(
        long tokenId,
        DateTime reuseDetectedAt,
        CancellationToken cancellationToken = default)
    {
        await RefreshTokens
            .Where(r => r.Id == tokenId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(b => b.ReuseDetectedAt, reuseDetectedAt),
                cancellationToken);
    }

    public void AddRefreshToken(RefreshToken token)
    {
        RefreshTokens.Add(token);
    }
}
