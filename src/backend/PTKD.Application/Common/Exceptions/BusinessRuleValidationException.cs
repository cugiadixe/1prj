using System;

namespace PTKD.Application.Common.Exceptions;

public class BusinessRuleValidationException : Exception
{
    public string ErrorCode { get; }

    public BusinessRuleValidationException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }
}
