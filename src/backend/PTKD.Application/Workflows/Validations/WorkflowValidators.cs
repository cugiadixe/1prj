using FluentValidation;
using PTKD.Application.Workflows.DTOs;

namespace PTKD.Application.Workflows.Validations;

public class CreateWorkflowDefinitionRequestValidator : AbstractValidator<CreateWorkflowDefinitionRequest>
{
    public CreateWorkflowDefinitionRequestValidator()
    {
        RuleFor(x => x.DefinitionCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.DefinitionName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.ProcessCode).NotEmpty().MaximumLength(100);
    }
}

public class UpdateWorkflowDefinitionRequestValidator : AbstractValidator<UpdateWorkflowDefinitionRequest>
{
    public UpdateWorkflowDefinitionRequestValidator()
    {
        RuleFor(x => x.DefinitionName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}

public class CreateWorkflowStepRequestValidator : AbstractValidator<CreateWorkflowStepRequest>
{
    public CreateWorkflowStepRequestValidator()
    {
        RuleFor(x => x.StepName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.StepOrder).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DueDurationMinutes).GreaterThan(0).When(x => x.DueDurationMinutes.HasValue);
    }
}

public class UpdateWorkflowStepRequestValidator : AbstractValidator<UpdateWorkflowStepRequest>
{
    public UpdateWorkflowStepRequestValidator()
    {
        RuleFor(x => x.StepName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.StepOrder).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.DueDurationMinutes).GreaterThan(0).When(x => x.DueDurationMinutes.HasValue);
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}

public class CreateApproverRuleRequestValidator : AbstractValidator<CreateApproverRuleRequest>
{
    public CreateApproverRuleRequestValidator()
    {
        RuleFor(x => x.ApproverSourceType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ApproverSourceValue).NotEmpty().MaximumLength(500);
    }
}

public class PublishVersionRequestValidator : AbstractValidator<PublishVersionRequest>
{
    public PublishVersionRequestValidator()
    {
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}

public class CreateWorkflowBindingRequestValidator : AbstractValidator<CreateWorkflowBindingRequest>
{
    public CreateWorkflowBindingRequestValidator()
    {
        RuleFor(x => x.WorkflowVersionId).GreaterThan(0);
        RuleFor(x => x.ProcessCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScopeType).NotEmpty().Must(s => s == "GLOBAL" || s == "COMPANY").WithMessage("ScopeType must be GLOBAL or COMPANY.");
        RuleFor(x => x.EffectiveFrom).NotEmpty();
    }
}

public class UpdateWorkflowBindingRequestValidator : AbstractValidator<UpdateWorkflowBindingRequest>
{
    public UpdateWorkflowBindingRequestValidator()
    {
        RuleFor(x => x.EffectiveFrom).NotEmpty();
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}

public class CreateWorkflowInstanceRequestValidator : AbstractValidator<CreateWorkflowInstanceRequest>
{
    public CreateWorkflowInstanceRequestValidator()
    {
        RuleFor(x => x.ProcessCode).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessEntityType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessEntityId).GreaterThan(0);
        RuleFor(x => x.PayloadJson).NotEmpty();
    }
}

public class ApprovalActionRequestValidator : AbstractValidator<ApprovalActionRequest>
{
    public ApprovalActionRequestValidator()
    {
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}

public class ReassignStepRequestValidator : AbstractValidator<ReassignStepRequest>
{
    public ReassignStepRequestValidator()
    {
        RuleFor(x => x.NewAssigneeUserId).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.TargetVersion).NotEmpty();
    }
}
