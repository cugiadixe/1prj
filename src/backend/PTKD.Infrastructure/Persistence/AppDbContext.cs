using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Persistence;

public class AppDbContext : DbContext, IOrganizationDbContext
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
}
