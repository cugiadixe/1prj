using System.Collections.Generic;
using System.Threading.Tasks;
using PTKD.Application.Organizations.Companies.DTOs;

namespace PTKD.Application.Organizations.Companies.Services;

public interface ICompanyService
{
    Task<CompanyDto> CreateCompanyAsync(CreateCompanyRequest request);
    Task<CompanyDto> UpdateCompanyAsync(long id, UpdateCompanyRequest request);
    Task<CompanyDto> UpdateCompanyStatusAsync(long id, UpdateCompanyStatusRequest request);
    Task<CompanyDto?> GetCompanyByIdAsync(long id);
    Task<IEnumerable<CompanyDto>> GetCompaniesAsync(); // We will simplify pagination for now or add it if needed
}
