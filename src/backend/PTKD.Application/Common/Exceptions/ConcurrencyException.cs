using System;

namespace PTKD.Application.Common.Exceptions;

public class ConcurrencyException : Exception
{
    public string ErrorCode { get; }

    public ConcurrencyException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
