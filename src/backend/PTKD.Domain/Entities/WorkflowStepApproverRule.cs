using System;

namespace PTKD.Domain.Entities;

public class WorkflowStepApproverRule
{
    public long Id { get; private set; }
    public long WorkflowStepId { get; private set; }
    public string ApproverSourceType { get; private set; } = null!;
    public string ApproverSourceValue { get; private set; } = null!;
    public int Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public WorkflowStep Step { get; private set; } = null!;

    private WorkflowStepApproverRule() { }

    public WorkflowStepApproverRule(long workflowStepId, string approverSourceType, string approverSourceValue, int priority = 0)
    {
        WorkflowStepId = workflowStepId;
        ApproverSourceType = approverSourceType ?? throw new ArgumentNullException(nameof(approverSourceType));
        ApproverSourceValue = approverSourceValue ?? throw new ArgumentNullException(nameof(approverSourceValue));
        Priority = priority;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sửa luật người duyệt. Trước đây chỉ thêm được: gõ sai một luật là phải xoá cả bước
    /// rồi dựng lại từ đầu.
    /// </summary>
    public void Update(string approverSourceType, string approverSourceValue, int priority)
    {
        ApproverSourceType = approverSourceType ?? throw new ArgumentNullException(nameof(approverSourceType));
        ApproverSourceValue = approverSourceValue ?? throw new ArgumentNullException(nameof(approverSourceValue));
        Priority = priority;
    }
}
