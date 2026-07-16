using System.Data;
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
}
