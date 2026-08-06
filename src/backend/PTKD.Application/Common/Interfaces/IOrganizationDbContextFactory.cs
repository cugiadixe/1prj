using PTKD.Application.Common.Interfaces;

namespace PTKD.Application.Common.Interfaces;

public interface IOrganizationDbContextFactory
{
    IOrganizationDbContext CreateDbContext();
}
