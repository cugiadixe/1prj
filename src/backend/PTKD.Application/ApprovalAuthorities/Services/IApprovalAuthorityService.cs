using System.Threading;
using System.Threading.Tasks;
using PTKD.Application.ApprovalAuthorities.DTOs;

namespace PTKD.Application.ApprovalAuthorities.Services;

public interface IApprovalAuthorityService
{
    Task<ApprovalAuthorityDto[]> ListAsync(long? companyId, long? departmentId, bool includeClosed, CancellationToken ct = default);
    Task<ApprovalAuthorityDto> CreateAsync(CreateApprovalAuthorityRequest request, long actorUserId, CancellationToken ct = default);
    Task<ApprovalAuthorityDto> CloseAsync(long id, CloseApprovalAuthorityRequest request, long actorUserId, CancellationToken ct = default);
}
