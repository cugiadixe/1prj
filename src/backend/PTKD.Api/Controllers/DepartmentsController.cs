using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PTKD.Application.Organizations.Departments.DTOs;
using PTKD.Application.Organizations.Departments.Services;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/organizations/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        var department = await _departmentService.CreateDepartmentAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = department.Id }, department);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateDepartmentRequest request)
    {
        var department = await _departmentService.UpdateDepartmentAsync(id, request);
        return Ok(department);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateDepartmentStatusRequest request)
    {
        var department = await _departmentService.UpdateDepartmentStatusAsync(id, request);
        return Ok(department);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var department = await _departmentService.GetDepartmentByIdAsync(id);
        if (department == null) return NotFound();
        return Ok(department);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] long companyId)
    {
        var departments = await _departmentService.GetDepartmentsAsync(companyId);
        return Ok(departments);
    }
}
