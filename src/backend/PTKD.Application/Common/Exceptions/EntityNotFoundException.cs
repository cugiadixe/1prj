using System;

namespace PTKD.Application.Common.Exceptions;

public class EntityNotFoundException : Exception
{
    public string ErrorCode { get; }

    public EntityNotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
