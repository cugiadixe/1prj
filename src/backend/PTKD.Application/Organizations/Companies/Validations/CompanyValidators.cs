using FluentValidation;
using PTKD.Application.Organizations.Companies.DTOs;

namespace PTKD.Application.Organizations.Companies.Validations;

public class CreateCompanyRequestValidator : AbstractValidator<CreateCompanyRequest>
{
    public CreateCompanyRequestValidator()
    {
        RuleFor(x => x.CompanyCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxCode).MaximumLength(50);
    }
}

public class UpdateCompanyRequestValidator : AbstractValidator<UpdateCompanyRequest>
{
    public UpdateCompanyRequestValidator()
    {
        RuleFor(x => x.CompanyCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxCode).MaximumLength(50);
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}

public class UpdateCompanyStatusRequestValidator : AbstractValidator<UpdateCompanyStatusRequest>
{
    public UpdateCompanyStatusRequestValidator()
    {
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}
