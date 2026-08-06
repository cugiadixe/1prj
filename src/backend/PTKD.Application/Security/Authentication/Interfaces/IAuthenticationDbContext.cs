using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PTKD.Domain.Entities;

namespace PTKD.Application.Security.Authentication.Interfaces;

public interface IAuthenticationDbContext : IDisposable, IAsyncDisposable
{
    DbSet<UserAuthAccount> UserAuthAccounts { get; }
    DbSet<PasswordHistory> PasswordHistories { get; }
    DbSet<User> Users { get; }

    Task<UserAuthAccount?> FindAccountByProviderForUpdateAsync(
        string providerType,
        string providerSubject,
        CancellationToken cancellationToken = default);

    Task<UserAuthAccount?> FindAccountByIdForUpdateAsync(
        long accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PasswordHistory>> GetRecentPasswordHistoryAsync(
        long accountId,
        int count,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default);
    IExecutionStrategy CreateExecutionStrategy();

    // Returns the underlying ADO.NET connection used by this EF context instance.
    // Callers must not open or close it; EF owns the connection lifetime.
    DbConnection GetDbConnection();

    // Returns the ADO.NET DbTransaction that is active for the current EF context
    // transaction, or null if no transaction is in progress.
    DbTransaction? GetCurrentDbTransaction();
}
