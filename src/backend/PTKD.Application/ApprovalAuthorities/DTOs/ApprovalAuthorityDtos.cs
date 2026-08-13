using System;

namespace PTKD.Application.ApprovalAuthorities.DTOs;

public class ApprovalAuthorityDto
{
    public long Id { get; set; }
    public long CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public long DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? ProcessCode { get; set; }
    public long ApproverUserId { get; set; }
    public string? ApproverName { get; set; }
    public int AuthorityLevel { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public long? DelegatedFromUserId { get; set; }
    public string? DelegatedFromName { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public string RowVersion { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateApprovalAuthorityRequest
{
    public long CompanyId { get; set; }
    public long DepartmentId { get; set; }
    /// <summary>NULL = áp cho mọi quy trình.</summary>
    public string? ProcessCode { get; set; }
    public long ApproverUserId { get; set; }
    /// <summary>1 = Trưởng phòng, 2 = Giám đốc…</summary>
    public int AuthorityLevel { get; set; } = 1;
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    /// <summary>Nếu là dòng uỷ quyền thay người khác: id người uỷ quyền gốc.</summary>
    public long? DelegatedFromUserId { get; set; }
    public string? Notes { get; set; }
}

public class CloseApprovalAuthorityRequest
{
    public DateTime EffectiveTo { get; set; }
}
