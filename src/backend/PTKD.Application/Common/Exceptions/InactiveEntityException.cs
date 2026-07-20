using System;

namespace PTKD.Application.Common.Exceptions;

/// <summary>
/// Thrown when an operation references an inactive permission or assignment.
/// Maps to HTTP 422 Unprocessable Entity (OD-D-B-14).
/// </summary>
public class InactiveEntityException : Exception
{
    public string ErrorCode { get; }

    public InactiveEntityException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
