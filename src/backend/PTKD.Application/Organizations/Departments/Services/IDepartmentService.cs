using System.Collections.Generic;
using System.Threading.Tasks;
using PTKD.Application.Organizations.Departments.DTOs;

namespace PTKD.Application.Organizations.Departments.Services;

public interface IDepartmentService
{
    Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request);
    Task<DepartmentDto> UpdateDepartmentAsync(long id, UpdateDepartmentRequest request);
    Task<DepartmentDto> UpdateDepartmentStatusAsync(long id, UpdateDepartmentStatusRequest request);
    Task<DepartmentDto?> GetDepartmentByIdAsync(long id);
    Task<IEnumerable<DepartmentDto>> GetDepartmentsAsync(long companyId);
}
