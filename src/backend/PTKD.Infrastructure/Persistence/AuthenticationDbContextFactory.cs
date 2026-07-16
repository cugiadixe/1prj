using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Infrastructure.Persistence;

public sealed class AuthenticationDbContextFactory : IAuthenticationDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;

    public AuthenticationDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public IAuthenticationDbContext CreateDbContext() => new AppDbContext(_options);
}
