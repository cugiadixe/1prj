using System.Threading.Tasks;
using PTKD.Application.Organizations.Assignments.DTOs;

namespace PTKD.Application.Organizations.Assignments.Services;

public interface IUserAssignmentService
{
    Task AssignCompanyAsync(long userId, AssignCompanyRequest request);
    Task AssignDepartmentAsync(long userId, AssignDepartmentRequest request);
    Task ChangePrimaryCompanyAsync(long userId, long companyAssignmentId, ChangePrimaryCompanyRequest request);
    Task ChangePrimaryDepartmentAsync(long userId, long departmentAssignmentId, ChangePrimaryDepartmentRequest request);
    Task CloseCompanyAssignmentAsync(long userId, long companyAssignmentId, CloseCompanyAssignmentRequest request);
    Task SameCompanyDepartmentTransferAsync(long userId, long companyAssignmentId, SameCompanyDepartmentTransferRequest request);
    Task CrossCompanyTransferAsync(long userId, long sourceCompanyAssignmentId, CrossCompanyTransferRequest request);
}
