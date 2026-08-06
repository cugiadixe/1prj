namespace PTKD.Application.Security.Authentication.Interfaces;

public interface IUtcClock
{
    DateTime UtcNow { get; }
}
