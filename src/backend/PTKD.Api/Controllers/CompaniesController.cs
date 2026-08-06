using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PTKD.Api.Security.Authorization;
using PTKD.Application.Organizations.Companies.DTOs;
using PTKD.Application.Organizations.Companies.Services;
using PTKD.Application.Security.Authorization.Attributes;
using PTKD.Application.Security.Authorization.Models;

namespace PTKD.API.Controllers;

[ApiController]
[Route("api/v2/organizations/companies")]
[Authorize]
[RequirePermission(PermissionCodes.OrganizationCompanyManage, PermissionScope.Global)]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request)
    {
        var company = await _companyService.CreateCompanyAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCompanyRequest request)
    {
        var company = await _companyService.UpdateCompanyAsync(id, request);
        return Ok(company);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateCompanyStatusRequest request)
    {
        var company = await _companyService.UpdateCompanyStatusAsync(id, request);
        return Ok(company);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id)
    {
        var company = await _companyService.GetCompanyByIdAsync(id);
        if (company == null) return NotFound();
        return Ok(company);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var companies = await _companyService.GetCompaniesAsync();
        return Ok(companies);
    }
}
