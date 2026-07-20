using System;

namespace PTKD.Application.Common.Exceptions;

/// <summary>
/// Thrown when the acting user lacks the required permission for an operation.
/// Maps to HTTP 403 Forbidden (OD-D-B-02).
/// </summary>
public class PermissionDeniedException : Exception
{
    public string ErrorCode { get; }

    public PermissionDeniedException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
