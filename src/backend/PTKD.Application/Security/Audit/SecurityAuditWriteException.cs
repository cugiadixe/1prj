using System;

namespace PTKD.Application.Security.Audit;

// Fail-closed typed exception for audit write failures (OD-F-04).
// Public Message is always the fixed sanitized string below — it never includes
// SQL text, connection string, parameter values, payload JSON, or credential material.
// The originating cause is preserved in InnerException for diagnostic log sinks only.
public sealed class SecurityAuditWriteException : Exception
{
    public SecurityAuditWriteException()
        : base("Security audit event could not be written.")
    {
    }

    public SecurityAuditWriteException(Exception innerException)
        : base("Security audit event could not be written.", innerException)
    {
    }
}
