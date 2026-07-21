using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Security.Audit;

public interface IAuditWriter
{
    // Fail-closed (OD-F-04): if write fails, the exception must propagate.
    // Callers must not treat the protected operation as successful if this throws.
    Task WriteAsync(SecurityAuditEventRecord record, CancellationToken cancellationToken = default);
}
