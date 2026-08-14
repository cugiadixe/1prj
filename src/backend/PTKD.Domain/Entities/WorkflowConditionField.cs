using System;

namespace PTKD.Domain.Entities;

/// <summary>
/// Trường được phép dùng làm điều kiện của một quy trình. DEV khai báo trước bằng migration;
/// admin chỉ CHỌN từ danh sách này chứ không gõ tên trường tự do — đây là ranh giới quản trị
/// giữ cho cấu hình không thể tham chiếu dữ liệu tuỳ ý.
///
/// <see cref="FieldCode"/> phải khớp chính xác tên thuộc tính trong payload_json của hồ sơ.
/// </summary>
public class WorkflowConditionField
{
    public const string TypeNumber = "NUMBER";
    public const string TypeText = "TEXT";
    public const string TypeBoolean = "BOOLEAN";
    public const string TypeDate = "DATE";

    public long Id { get; private set; }
    public string ProcessCode { get; private set; } = null!;
    public string FieldCode { get; private set; } = null!;
    public string FieldLabel { get; private set; } = null!;
    public string DataType { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WorkflowConditionField() { }
}
