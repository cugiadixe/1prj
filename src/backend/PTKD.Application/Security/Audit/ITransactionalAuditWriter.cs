using System.Data.Common;

namespace PTKD.Application.Security.Audit;

// Transaction-aware audit writer for operations that must be atomic with a password
// change or other single-transaction mutation (OD-F-04, blocker resolution).
//
// The caller is responsible for supplying the same DbConnection and DbTransaction that
// the surrounding operation is using.  The implementation MUST NOT open a new connection
// or begin a new transaction.  If the write fails the exception propagates and the
// caller's transaction must roll back.
public interface ITransactionalAuditWriter
{
    Task WriteAsync(
        SecurityAuditEventRecord record,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken cancellationToken = default);
}
