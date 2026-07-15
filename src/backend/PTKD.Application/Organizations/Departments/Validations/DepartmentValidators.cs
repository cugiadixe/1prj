using FluentValidation;
using PTKD.Application.Organizations.Departments.DTOs;

namespace PTKD.Application.Organizations.Departments.Validations;

public class CreateDepartmentRequestValidator : AbstractValidator<CreateDepartmentRequest>
{
    public CreateDepartmentRequestValidator()
    {
        RuleFor(x => x.DepartmentCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CompanyId).GreaterThan(0);
    }
}

public class UpdateDepartmentRequestValidator : AbstractValidator<UpdateDepartmentRequest>
{
    public UpdateDepartmentRequestValidator()
    {
        RuleFor(x => x.DepartmentCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}

public class UpdateDepartmentStatusRequestValidator : AbstractValidator<UpdateDepartmentStatusRequest>
{
    public UpdateDepartmentStatusRequestValidator()
    {
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}
