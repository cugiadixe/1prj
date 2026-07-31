using System.Threading;
using System.Threading.Tasks;

namespace PTKD.Application.Workflows.Services;

public interface IApproverResolver
{
    Task<long[]> ResolveApproversAsync(string approverSourceType, string approverSourceValue, long requesterId, long? companyId, CancellationToken ct = default);
}
