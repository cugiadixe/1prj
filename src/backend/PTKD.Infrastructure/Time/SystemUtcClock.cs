using PTKD.Application.Security.Authentication.Interfaces;

namespace PTKD.Infrastructure.Time;

public sealed class SystemUtcClock : IUtcClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
