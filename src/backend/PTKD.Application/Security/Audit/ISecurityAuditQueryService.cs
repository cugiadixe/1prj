using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.Common.Models;
using PTKD.Application.Security.Audit.DTOs;

namespace PTKD.Application.Security.Audit;

public interface ISecurityAuditQueryService
{
    Task<PagedResult<SecurityAuditEventDto>> GetAuditEventsAsync(SecurityAuditQueryParameters parameters, CancellationToken cancellationToken = default);
}
