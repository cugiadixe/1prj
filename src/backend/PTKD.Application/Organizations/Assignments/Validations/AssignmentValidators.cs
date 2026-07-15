using FluentValidation;
using PTKD.Application.Organizations.Assignments.DTOs;

namespace PTKD.Application.Organizations.Assignments.Validations;

public class AssignCompanyRequestValidator : AbstractValidator<AssignCompanyRequest>
{
    public AssignCompanyRequestValidator()
    {
        RuleFor(x => x.CompanyId).GreaterThan(0);
        RuleFor(x => x.PrimaryDepartmentId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class AssignDepartmentRequestValidator : AbstractValidator<AssignDepartmentRequest>
{
    public AssignDepartmentRequestValidator()
    {
        RuleFor(x => x.UserCompanyAssignmentId).GreaterThan(0);
        RuleFor(x => x.CompanyAssignmentRowVersion).NotEmpty();
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class ChangePrimaryCompanyRequestValidator : AbstractValidator<ChangePrimaryCompanyRequest>
{
    public ChangePrimaryCompanyRequestValidator()
    {
        RuleFor(x => x.TargetRowVersion).NotEmpty();
        RuleFor(x => x.CurrentPrimaryAssignmentId).GreaterThan(0);
        RuleFor(x => x.CurrentPrimaryRowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class ChangePrimaryDepartmentRequestValidator : AbstractValidator<ChangePrimaryDepartmentRequest>
{
    public ChangePrimaryDepartmentRequestValidator()
    {
        RuleFor(x => x.TargetRowVersion).NotEmpty();
        RuleFor(x => x.CurrentPrimaryAssignmentId).GreaterThan(0);
        RuleFor(x => x.CurrentPrimaryRowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class CloseCompanyAssignmentRequestValidator : AbstractValidator<CloseCompanyAssignmentRequest>
{
    public CloseCompanyAssignmentRequestValidator()
    {
        RuleFor(x => x.CompanyAssignmentRowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class SameCompanyDepartmentTransferRequestValidator : AbstractValidator<SameCompanyDepartmentTransferRequest>
{
    public SameCompanyDepartmentTransferRequestValidator()
    {
        RuleFor(x => x.CompanyAssignmentRowVersion).NotEmpty();
        RuleFor(x => x.SourceDepartmentAssignmentId).GreaterThan(0);
        RuleFor(x => x.SourceDepartmentAssignmentRowVersion).NotEmpty();
        RuleFor(x => x.TargetDepartmentId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class CrossCompanyTransferRequestValidator : AbstractValidator<CrossCompanyTransferRequest>
{
    public CrossCompanyTransferRequestValidator()
    {
        RuleFor(x => x.SourceCompanyAssignmentRowVersion).NotEmpty();
        RuleFor(x => x.TargetCompanyId).GreaterThan(0);
        RuleFor(x => x.TargetDepartmentId).GreaterThan(0);
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
