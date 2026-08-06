namespace PTKD.Application.Security.Authentication.Interfaces;

/// <summary>
/// Creates ITokenSessionDbContext instances. One instance per unit of work.
/// </summary>
public interface ITokenSessionDbContextFactory
{
    ITokenSessionDbContext CreateDbContext();
}
