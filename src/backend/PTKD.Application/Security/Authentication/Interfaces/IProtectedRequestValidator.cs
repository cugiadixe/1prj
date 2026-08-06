using System;
using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Security.Authentication.Interfaces;

public interface IProtectedRequestValidator
{
    Task<bool> ValidateAsync(
        long userId,
        Guid securityStamp,
        DateTime issuedAtUtc,
        CancellationToken cancellationToken = default);
}
