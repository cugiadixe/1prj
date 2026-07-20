using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Organizations.Assignments.DTOs;
using PTKD.Application.Organizations.Assignments.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/organizations/users/{userId}")]
[Authorize]
[RequirePermission(PermissionCodes.OrganizationUserManage, PermissionScope.Global)]
public class UserAssignmentsController : ControllerBase
{
    private readonly IUserAssignmentService _assignmentService;

    public UserAssignmentsController(IUserAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpPost("companies")]
    public async Task<IActionResult> AssignCompany(long userId, [FromBody] AssignCompanyRequest request)
    {
        await _assignmentService.AssignCompanyAsync(userId, request);
        return NoContent();
    }

    [HttpPost("departments")]
    public async Task<IActionResult> AssignDepartment(long userId, [FromBody] AssignDepartmentRequest request)
    {
        await _assignmentService.AssignDepartmentAsync(userId, request);
        return NoContent();
    }

    [HttpPut("company-assignments/{companyAssignmentId}/primary")]
    public async Task<IActionResult> ChangePrimaryCompany(long userId, long companyAssignmentId, [FromBody] ChangePrimaryCompanyRequest request)
    {
        await _assignmentService.ChangePrimaryCompanyAsync(userId, companyAssignmentId, request);
        return NoContent();
    }

    [HttpPut("department-assignments/{departmentAssignmentId}/primary")]
    public async Task<IActionResult> ChangePrimaryDepartment(long userId, long departmentAssignmentId, [FromBody] ChangePrimaryDepartmentRequest request)
    {
        await _assignmentService.ChangePrimaryDepartmentAsync(userId, departmentAssignmentId, request);
        return NoContent();
    }

    [HttpPut("company-assignments/{companyAssignmentId}/close")]
    public async Task<IActionResult> CloseCompanyAssignment(long userId, long companyAssignmentId, [FromBody] CloseCompanyAssignmentRequest request)
    {
        await _assignmentService.CloseCompanyAssignmentAsync(userId, companyAssignmentId, request);
        return NoContent();
    }

    [HttpPost("company-assignments/{companyAssignmentId}/transfer/same-company")]
    public async Task<IActionResult> SameCompanyDepartmentTransfer(long userId, long companyAssignmentId, [FromBody] SameCompanyDepartmentTransferRequest request)
    {
        await _assignmentService.SameCompanyDepartmentTransferAsync(userId, companyAssignmentId, request);
        return NoContent();
    }

    [HttpPost("company-assignments/{sourceCompanyAssignmentId}/transfer/cross-company")]
    public async Task<IActionResult> CrossCompanyTransfer(long userId, long sourceCompanyAssignmentId, [FromBody] CrossCompanyTransferRequest request)
    {
        await _assignmentService.CrossCompanyTransferAsync(userId, sourceCompanyAssignmentId, request);
        return NoContent();
    }
}
