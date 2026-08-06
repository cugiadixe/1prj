using FluentValidation;
using PTKD.Application.Organizations.Users.DTOs;

namespace PTKD.Application.Organizations.Users.Validations;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.EmploymentStatus).NotEmpty().MaximumLength(30);
        RuleFor(x => x.AccountStatus).NotEmpty().MaximumLength(30);
        RuleFor(x => x.InitialCompanyId).GreaterThan(0);
        RuleFor(x => x.InitialDepartmentId).GreaterThan(0);
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.EmployeeCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.EmploymentStatus).NotEmpty().MaximumLength(30);
        RuleFor(x => x.AccountStatus).NotEmpty().MaximumLength(30);
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}
