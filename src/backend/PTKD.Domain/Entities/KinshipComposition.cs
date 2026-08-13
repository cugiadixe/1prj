namespace PTKD.Domain.Entities;

/// <summary>
/// Bảng tra suy diễn 2 bậc: owner→pivot = KindA và pivot→target = KindB, giới
/// tính người trung gian = PivotGender ⇒ owner→target = ResultKind.
/// PivotGender = 'ANY' khi giới tính người trung gian không ảnh hưởng.
/// Bảng tham chiếu — chỉ đọc.
/// </summary>
public class KinshipComposition
{
    public const string PivotAny = "ANY";

    public string KindA { get; private set; } = null!;
    public string KindB { get; private set; } = null!;
    public string PivotGender { get; private set; } = null!;
    public string ResultKind { get; private set; } = null!;
    public bool NeedsConfirmation { get; private set; }
    public string? Note { get; private set; }

    private KinshipComposition() { }
}
