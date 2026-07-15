using Microsoft.EntityFrameworkCore;
using PTKD.Application.Common.Interfaces;

namespace PTKD.Infrastructure.Persistence;

public class AppDbContextFactory : IOrganizationDbContextFactory
{
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextFactory(DbContextOptions<AppDbContext> options)
    {
        _options = options;
    }

    public IOrganizationDbContext CreateDbContext()
    {
        return new AppDbContext(_options);
    }
}
