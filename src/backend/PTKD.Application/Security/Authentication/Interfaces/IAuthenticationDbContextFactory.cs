namespace PTKD.Application.Security.Authentication.Interfaces;

public interface IAuthenticationDbContextFactory
{
    IAuthenticationDbContext CreateDbContext();
}
