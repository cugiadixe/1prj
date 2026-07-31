using System;

namespace PTKD.Domain.Entities;

public class WorkflowCondition
{
    public long Id { get; private set; }
    public long WorkflowVersionId { get; private set; }
    public string FieldCode { get; private set; } = null!;
    public string Operator { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    public WorkflowDefinitionVersion Version { get; private set; } = null!;

    private WorkflowCondition() { }

    public WorkflowCondition(long workflowVersionId, string fieldCode, string operatorValue, string value)
    {
        WorkflowVersionId = workflowVersionId;
        FieldCode = fieldCode ?? throw new ArgumentNullException(nameof(fieldCode));
        Operator = operatorValue ?? throw new ArgumentNullException(nameof(operatorValue));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        CreatedAt = DateTime.UtcNow;
    }
}
