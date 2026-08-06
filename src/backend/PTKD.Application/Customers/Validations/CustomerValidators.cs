using FluentValidation;
using PTKD.Application.Customers.DTOs;

namespace PTKD.Application.Customers.Validations;

public class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.CustomerCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cccd).MaximumLength(20);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.TaxCode).MaximumLength(20);
        RuleFor(x => x.PermanentAddress).MaximumLength(500);
        RuleFor(x => x.ContactAddress).MaximumLength(500);
        RuleFor(x => x.DobPrecision).Must(v => v == null || v is "FULL" or "YEAR_MONTH" or "YEAR" or "UNKNOWN")
            .WithMessage("DobPrecision must be FULL, YEAR_MONTH, YEAR, or UNKNOWN.");
        RuleFor(x => x.Gender).Must(v => v == null || v is "MALE" or "FEMALE" or "OTHER")
            .WithMessage("Gender must be MALE, FEMALE, or OTHER.");
        RuleFor(x => x.InternalNotes).MaximumLength(2000);
    }
}

public class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TargetVersion).NotEmpty();
        RuleFor(x => x.Cccd).MaximumLength(20);
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.TaxCode).MaximumLength(20);
        RuleFor(x => x.PermanentAddress).MaximumLength(500);
        RuleFor(x => x.ContactAddress).MaximumLength(500);
        RuleFor(x => x.DobPrecision).Must(v => v == null || v is "FULL" or "YEAR_MONTH" or "YEAR" or "UNKNOWN")
            .WithMessage("DobPrecision must be FULL, YEAR_MONTH, YEAR, or UNKNOWN.");
        RuleFor(x => x.Gender).Must(v => v == null || v is "MALE" or "FEMALE" or "OTHER")
            .WithMessage("Gender must be MALE, FEMALE, or OTHER.");
    }
}

public class CreateCustomerCompanyContextRequestValidator : AbstractValidator<CreateCustomerCompanyContextRequest>
{
    public CreateCustomerCompanyContextRequestValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.InternalNotes).MaximumLength(2000);
    }
}

public class UpdateCustomerCompanyContextRequestValidator : AbstractValidator<UpdateCustomerCompanyContextRequest>
{
    public UpdateCustomerCompanyContextRequestValidator()
    {
        RuleFor(x => x.RelationshipStatus).NotEmpty().Must(v => v is "ACTIVE" or "INACTIVE")
            .WithMessage("RelationshipStatus must be ACTIVE or INACTIVE.");
        RuleFor(x => x.TargetVersion).NotEmpty();
        RuleFor(x => x.InternalNotes).MaximumLength(2000);
    }
}
