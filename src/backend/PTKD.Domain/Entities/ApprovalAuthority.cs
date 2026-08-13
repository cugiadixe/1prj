using System;

namespace PTKD.Domain.Entities;

/// <summary>
/// Thẩm quyền phê duyệt — nguồn dữ liệu độc lập cho việc "ai được duyệt cái gì".
/// KHÔNG phải engine quy trình: engine vẫn quyết định luồng chạy, bảng này chỉ trả lời
/// "ở công ty/phòng ban này, cấp thẩm quyền X, người duyệt là ai".
///
/// Một dòng = một người duyệt, cho một (công ty, phòng ban, cấp), tuỳ chọn giới hạn theo
/// mã quy trình và ngưỡng tiền, trong một khoảng hiệu lực.
/// - Nghỉ phép: đặt EffectiveTo cho dòng của trưởng phòng, thêm dòng cho người thay
///   với DelegatedFromUserId trỏ về trưởng phòng (ngữ nghĩa THAY THẾ — D10).
/// - Nhiều cấp (D7): thêm dòng AuthorityLevel = 2 (Giám đốc) là xong, không sửa code.
/// </summary>
public class ApprovalAuthority
{
    public const string StatusActive = "ACTIVE";
    public const string StatusClosed = "CLOSED";

    public long Id { get; private set; }

    /// <summary>Công ty áp dụng (phạm vi theo công ty — D6).</summary>
    public long CompanyId { get; private set; }

    /// <summary>Phòng ban áp dụng — dùng để khớp "trưởng phòng của người tạo".</summary>
    public long DepartmentId { get; private set; }

    /// <summary>Mã quy trình áp dụng; NULL = áp cho mọi quy trình.</summary>
    public string? ProcessCode { get; private set; }

    /// <summary>Người được quyền duyệt.</summary>
    public long ApproverUserId { get; private set; }

    /// <summary>Cấp thẩm quyền: 1 = Trưởng phòng, 2 = Giám đốc… (D7).</summary>
    public int AuthorityLevel { get; private set; }

    /// <summary>Ngưỡng tiền tối thiểu áp dụng; NULL = không giới hạn dưới.</summary>
    public decimal? MinAmount { get; private set; }

    /// <summary>Ngưỡng tiền tối đa áp dụng; NULL = không giới hạn trên.</summary>
    public decimal? MaxAmount { get; private set; }

    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }

    /// <summary>Nếu dòng này là uỷ quyền thay người khác: id người uỷ quyền gốc; NULL = thẩm quyền thường.</summary>
    public long? DelegatedFromUserId { get; private set; }

    public string Status { get; private set; } = null!;
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private ApprovalAuthority() { } // EF Core

    public static ApprovalAuthority Create(
        long companyId,
        long departmentId,
        string? processCode,
        long approverUserId,
        int authorityLevel,
        decimal? minAmount,
        decimal? maxAmount,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        long? delegatedFromUserId,
        string? notes,
        long createdByUserId)
    {
        if (authorityLevel <= 0)
            throw new ArgumentException("Authority level must be positive.", nameof(authorityLevel));
        if (minAmount.HasValue && minAmount.Value < 0)
            throw new ArgumentException("Min amount cannot be negative.", nameof(minAmount));
        if (maxAmount.HasValue && minAmount.HasValue && maxAmount.Value < minAmount.Value)
            throw new ArgumentException("Max amount cannot be less than min amount.", nameof(maxAmount));
        if (effectiveTo.HasValue && effectiveTo.Value <= effectiveFrom)
            throw new ArgumentException("EffectiveTo must be strictly greater than EffectiveFrom.", nameof(effectiveTo));

        return new ApprovalAuthority
        {
            CompanyId = companyId,
            DepartmentId = departmentId,
            ProcessCode = processCode,
            ApproverUserId = approverUserId,
            AuthorityLevel = authorityLevel,
            MinAmount = minAmount,
            MaxAmount = maxAmount,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            DelegatedFromUserId = delegatedFromUserId,
            Status = StatusActive,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Đóng dòng thẩm quyền tại một thời điểm (dùng cho nghỉ phép / thu hồi).</summary>
    public void Close(DateTime effectiveTo, long updatedByUserId)
    {
        if (Status != StatusActive)
            throw new InvalidOperationException("Only active authorities can be closed.");
        if (effectiveTo <= EffectiveFrom)
            throw new InvalidOperationException("EffectiveTo must be strictly greater than EffectiveFrom.");

        Status = StatusClosed;
        EffectiveTo = effectiveTo;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
