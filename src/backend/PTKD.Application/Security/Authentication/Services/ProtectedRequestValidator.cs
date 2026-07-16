using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PTKD.Application.Security.Authentication.Interfaces;
using PTKD.Domain.Security.Authentication;

namespace PTKD.Application.Security.Authentication.Services;

public sealed class ProtectedRequestValidator : IProtectedRequestValidator
{
    private readonly IAuthenticationDbContextFactory _dbContextFactory;
    private readonly AuthenticationAccountPolicy _policy;
    private readonly ILogger<ProtectedRequestValidator> _logger;

    public ProtectedRequestValidator(
        IAuthenticationDbContextFactory dbContextFactory,
        AuthenticationAccountPolicy policy,
        ILogger<ProtectedRequestValidator> logger)
    {
        _dbContextFactory = dbContextFactory ?? throw new ArgumentNullException(nameof(dbContextFactory));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ValidateAsync(
        long userId,
        Guid securityStamp,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var context = _dbContextFactory.CreateDbContext();
            
            var account = await context.UserAuthAccounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

            if (account == null)
            {
                return false;
            }

            if (!string.Equals(account.AuthAccountStatus, AuthenticationAccountPolicy.ActiveAccountStatus, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!_policy.IsLinkedUserEligible(account.User))
            {
                return false;
            }

            if (account.SecurityStamp != securityStamp)
            {
                return false;
            }

            if (account.SessionsInvalidatedAt.HasValue && issuedAtUtc <= account.SessionsInvalidatedAt.Value)
            {
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Protected request validation failed due to an infrastructure exception.");
            // Fail closed on exception
            return false;
        }
    }
}
