using System;

namespace PTKD.Application.Workflows.DTOs;

public class BusinessProcessDto
{
    public string ProcessCode { get; set; } = null!;
    public string ProcessName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsApprovalRequired { get; set; }
    public bool IsActive { get; set; }
}

public class WorkflowDefinitionListItemDto
{
    public long Id { get; set; }
    public string DefinitionCode { get; set; } = null!;
    public string DefinitionName { get; set; } = null!;
    public string ProcessCode { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WorkflowDefinitionDetailDto
{
    public long Id { get; set; }
    public string DefinitionCode { get; set; } = null!;
    public string DefinitionName { get; set; } = null!;
    public string? Description { get; set; }
    public string ProcessCode { get; set; } = null!;
    public bool IsActive { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateWorkflowDefinitionRequest
{
    public string DefinitionCode { get; set; } = null!;
    public string DefinitionName { get; set; } = null!;
    public string? Description { get; set; }
    public string ProcessCode { get; set; } = null!;
}

public class UpdateWorkflowDefinitionRequest
{
    public string DefinitionName { get; set; } = null!;
    public string? Description { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class WorkflowVersionListItemDto
{
    public long Id { get; set; }
    public int VersionNumber { get; set; }
    public string VersionStatus { get; set; } = null!;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class WorkflowVersionDetailDto
{
    public long Id { get; set; }
    public long WorkflowDefinitionId { get; set; }
    public int VersionNumber { get; set; }
    public string VersionStatus { get; set; } = null!;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public WorkflowStepDto[] Steps { get; set; } = [];
    public WorkflowConditionDto[] Conditions { get; set; } = [];
}

public class CreateWorkflowVersionRequest
{
}

public class WorkflowStepDto
{
    public long Id { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public int? DueDurationMinutes { get; set; }
    public string RowVersion { get; set; } = null!;
    public ApproverRuleDto[] ApproverRules { get; set; } = [];
}

public class CreateWorkflowStepRequest
{
    public string StepName { get; set; } = null!;
    public int StepOrder { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? Description { get; set; }
    public int? DueDurationMinutes { get; set; }
}

public class UpdateWorkflowStepRequest
{
    public string StepName { get; set; } = null!;
    public int StepOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? Description { get; set; }
    public int? DueDurationMinutes { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class ApproverRuleDto
{
    public long Id { get; set; }
    public string ApproverSourceType { get; set; } = null!;
    public string ApproverSourceValue { get; set; } = null!;
    public int Priority { get; set; }
}

public class CreateApproverRuleRequest
{
    public string ApproverSourceType { get; set; } = null!;
    public string ApproverSourceValue { get; set; } = null!;
    public int Priority { get; set; }
}

public class WorkflowConditionDto
{
    public long Id { get; set; }
    public string FieldCode { get; set; } = null!;
    public string Operator { get; set; } = null!;
    public string Value { get; set; } = null!;
}

public class PublishVersionRequest
{
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class WorkflowBindingListItemDto
{
    public long Id { get; set; }
    public long WorkflowVersionId { get; set; }
    public string ProcessCode { get; set; } = null!;
    public string ScopeType { get; set; } = null!;
    public long? CompanyId { get; set; }
    public int Priority { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public string RowVersion { get; set; } = null!;
}

public class CreateWorkflowBindingRequest
{
    public long WorkflowVersionId { get; set; }
    public string ProcessCode { get; set; } = null!;
    public string ScopeType { get; set; } = null!;
    public long? CompanyId { get; set; }
    public int Priority { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class UpdateWorkflowBindingRequest
{
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class WorkflowInstanceDto
{
    public long Id { get; set; }
    public long WorkflowVersionId { get; set; }
    public string ProcessCode { get; set; } = null!;
    public long? CompanyId { get; set; }
    public long RequesterId { get; set; }
    public string? RequesterName { get; set; }
    public string BusinessEntityType { get; set; } = null!;
    public long BusinessEntityId { get; set; }
    public string? BusinessEntityLabel { get; set; }
    public string InstanceStatus { get; set; } = null!;
    public int RoundNo { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public WorkflowInstanceStepDto[] Steps { get; set; } = [];
}

public class CreateWorkflowInstanceRequest
{
    public string ProcessCode { get; set; } = null!;
    public string BusinessEntityType { get; set; } = null!;
    public long BusinessEntityId { get; set; }
    public long? CompanyId { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string? BeforeDataJson { get; set; }
}

public class WorkflowInstanceStepDto
{
    public long Id { get; set; }
    public int StepOrder { get; set; }
    public string StepName { get; set; } = null!;
    public int RoundNo { get; set; }
    public string StepStatus { get; set; } = null!;
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public long? CompletedBy { get; set; }
    public string? CompletedByName { get; set; }
    public string RowVersion { get; set; } = null!;
    public WorkflowInstanceStepAssigneeDto[] Assignees { get; set; } = [];
}

public class WorkflowInstanceStepAssigneeDto
{
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string ApproverSourceType { get; set; } = null!;
}

public class ApprovalActionRequest
{
    public string? Reason { get; set; }
    public string? Comment { get; set; }
    public string TargetVersion { get; set; } = null!;
}

public class ReassignStepRequest
{
    public long NewAssigneeUserId { get; set; }
    public string Reason { get; set; } = null!;
    public string TargetVersion { get; set; } = null!;
}

public class MyApprovalItemDto
{
    public long InstanceId { get; set; }
    public long StepId { get; set; }
    public string ProcessCode { get; set; } = null!;
    public string BusinessEntityType { get; set; } = null!;
    public long BusinessEntityId { get; set; }
    public string? BusinessEntityLabel { get; set; }
    public string StepName { get; set; } = null!;
    public string InstanceStatus { get; set; } = null!;
    public DateTime? AssignedAt { get; set; }
    public long RequesterId { get; set; }
    public string? RequesterName { get; set; }
}

public class WorkflowSearchRequest
{
    public string? ProcessCode { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class WorkflowActionDto
{
    public long Id { get; set; }
    public long WorkflowInstanceStepId { get; set; }
    public long WorkflowInstanceId { get; set; }
    public string ActionType { get; set; } = null!;
    public long ActedBy { get; set; }
    public string? ActedByName { get; set; }
    public long? OnBehalfOf { get; set; }
    public string? OnBehalfOfName { get; set; }
    public string? Reason { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RetryExecutionRequest
{
    public string TargetVersion { get; set; } = null!;
}

/// <summary>Một lựa chọn cho ô "giá trị nguồn" của luật người duyệt.</summary>
public class ApproverSourceOptionDto
{
    /// <summary>Giá trị lưu vào approver_source_value (id dạng chuỗi, hoặc mã code).</summary>
    public string Value { get; set; } = null!;
    /// <summary>Nhãn hiển thị cho người dùng.</summary>
    public string Label { get; set; } = null!;
    /// <summary>Thông tin phụ (vd mã nhân viên, phòng ban) — hiển thị mờ bên cạnh.</summary>
    public string? Hint { get; set; }
}

/// <summary>Tham số tra cứu hồ sơ quy trình cho màn hình quản trị.</summary>
public class WorkflowInstanceSearchRequest
{
    public string? ProcessCode { get; set; }
    public string? InstanceStatus { get; set; }
    public long? CompanyId { get; set; }
    public long? RequesterId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
