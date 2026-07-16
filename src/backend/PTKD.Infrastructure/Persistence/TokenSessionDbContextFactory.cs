using Microsoft.EntityFrameworkCore;
using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Infrastructure.Persistence;

public sealed class TokenSessionDbContextFactory : ITokenSessionDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TokenSessionDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ITokenSessionDbContext CreateDbContext() => new AppDbContext(_options);
}
