using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence;

public class AppDbContext : DbContext, IOrganizationDbContext, IAuthenticationDbContext
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
    public DbSet<UserAuthAccount> UserAuthAccounts => Set<UserAuthAccount>();
    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

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
}
