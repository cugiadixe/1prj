namespace PTKD.Application.Security.Authorization.DTOs;

public class UserCompanyDto
{
    public long CompanyId { get; set; }
    public string CompanyCode { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public bool IsDefault { get; set; }
}

public class UserCompaniesResponse
{
    public List<UserCompanyDto> Companies { get; set; } = new();
}
