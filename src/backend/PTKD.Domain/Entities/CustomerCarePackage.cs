using System;

namespace PTKD.Domain.Entities;

public class CustomerCarePackage
{
    public const string StatusPendingApproval = "PENDING_APPROVAL"; // chờ trưởng phòng duyệt (Nhóm C)
    public const string StatusPendingGrave = "PENDING_GRAVE"; // đã gán khách, chờ gán mộ
    public const string StatusActive = "ACTIVE";              // đã gán mộ, hiệu lực
    public const string StatusExpired = "EXPIRED";            // hết hạn
    public const string StatusCancelled = "CANCELLED";        // đã hủy

    public long Id { get; private set; }
    public long CustomerId { get; private set; }
    public long ServiceTypeId { get; private set; }
    public long? GraveId { get; private set; }
    public int CotCount { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string Status { get; private set; } = null!;
    public string? Notes { get; private set; }
    /// <summary>Hồ sơ quy trình phê duyệt gắn với gói (khi qua duyệt); NULL nếu gán thẳng.</summary>
    public long? WorkflowInstanceId { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public long? CreatedByUserId { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public long? UpdatedByUserId { get; private set; }

    private CustomerCarePackage() { }

    public static CustomerCarePackage Create(
        long customerId, long serviceTypeId,
        int cotCount, decimal unitPrice, DateTime startDate, DateTime? endDate,
        string? notes, long createdByUserId, bool requiresApproval = false)
    {
        if (cotCount <= 0)
            throw new ArgumentException("Cot count must be positive.", nameof(cotCount));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        return new CustomerCarePackage
        {
            CustomerId = customerId,
            ServiceTypeId = serviceTypeId,
            GraveId = null,
            CotCount = cotCount,
            UnitPrice = unitPrice,
            TotalPrice = unitPrice * cotCount,
            StartDate = startDate,
            EndDate = endDate,
            // Có quy trình phê duyệt → chờ duyệt; không thì sẵn sàng gán mộ như cũ.
            Status = requiresApproval ? StatusPendingApproval : StatusPendingGrave,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Gắn hồ sơ quy trình phê duyệt vừa sinh cho gói.</summary>
    public void SetWorkflowInstance(long workflowInstanceId, long updatedByUserId)
    {
        WorkflowInstanceId = workflowInstanceId;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    /// <summary>
    /// Duyệt xong (trưởng phòng duyệt hoặc tự động duyệt): chuyển từ chờ duyệt sang chờ gán mộ.
    /// Lúc này gói mới thực sự hiện cho khách để gán vào mộ (luồng c).
    /// </summary>
    public void MarkApproved(long updatedByUserId)
    {
        if (Status != StatusPendingApproval)
            throw new InvalidOperationException($"Only a package pending approval can be approved (current: {Status}).");

        Status = StatusPendingGrave;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    /// <summary>
    /// Trưởng phòng TỪ CHỐI: đưa gói ra khỏi trạng thái chờ duyệt.
    /// Dùng lại trạng thái CANCELLED (đã hủy) để không phải nới ràng buộc CSDL; lý do từ chối
    /// nằm ở nhật ký quy trình và được ghi thêm vào ghi chú của gói.
    /// </summary>
    /// <summary>Giới hạn cột notes trong CSDL (nvarchar(2000)).</summary>
    private const int NotesMaxLength = 2000;

    public void MarkRejected(long updatedByUserId, string? reason)
    {
        if (Status != StatusPendingApproval)
            throw new InvalidOperationException($"Only a package pending approval can be rejected (current: {Status}).");

        Status = StatusCancelled;

        var note = string.IsNullOrWhiteSpace(reason)
            ? "Bị từ chối phê duyệt."
            : $"Bị từ chối phê duyệt: {reason}";

        // Ghi chú cũ và lý do từ chối đều có thể dài tới 2000 ký tự, nối lại sẽ TRÀN cột.
        // Ưu tiên giữ ghi chú từ chối (thông tin mới nhất), cắt bớt phần cũ nếu cần.
        Notes = AppendWithinLimit(Notes, note);

        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    private static string AppendWithinLimit(string? existing, string addition)
    {
        // Bản thân phần thêm vào đã quá dài → cắt chính nó.
        if (addition.Length >= NotesMaxLength)
            return addition[..NotesMaxLength];

        if (string.IsNullOrWhiteSpace(existing))
            return addition;

        var combined = $"{existing}\n{addition}";
        if (combined.Length <= NotesMaxLength)
            return combined;

        // Cắt phần ghi chú cũ từ đầu, chừa đủ chỗ cho dấu xuống dòng + phần thêm vào.
        var keep = NotesMaxLength - addition.Length - 1;
        return keep <= 0 ? addition : $"{existing[..keep]}\n{addition}";
    }

    public void AssignGrave(long graveId, long updatedByUserId)
    {
        if (Status == StatusPendingApproval)
            throw new InvalidOperationException("Gói đang chờ duyệt, chưa thể gán vào mộ.");
        if (Status == StatusCancelled)
            throw new InvalidOperationException("Cannot assign a grave to a cancelled package.");

        GraveId = graveId;
        Status = StatusActive;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }

    public void Cancel(long updatedByUserId)
    {
        Status = StatusCancelled;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = updatedByUserId;
    }
}
