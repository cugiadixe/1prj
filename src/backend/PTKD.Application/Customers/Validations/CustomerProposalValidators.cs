using FluentValidation;
using PTKD.Application.Customers.DTOs;

namespace PTKD.Application.Customers.Validations;

public class CreateCustomerProposalRequestValidator : AbstractValidator<CreateCustomerProposalRequest>
{
    public CreateCustomerProposalRequestValidator()
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
