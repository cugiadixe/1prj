using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using PTKD.Application.Cards.Services;
using PTKD.Application.Common.Exceptions;
using PTKD.Application.Common.Interfaces;
using PTKD.Domain.Entities;

namespace PTKD.Infrastructure.Cards;

/// <summary>
/// Render thẻ mộ ra PDF khổ B5 ngang (250×176mm) gập đôi = 4 mặt:
///   Trang 1 (mặt ngoài): trái = QUÝ KHÁCH LƯU Ý, phải = bìa.
///   Trang 2 (mặt trong): trái = THÔNG TIN VỀ PHẦN MỘ, phải = THÔNG TIN VỀ CHỦ MỘ.
/// Bản đầu render đầy đủ khung + chữ (phôi master) để xem trước và in trực tiếp.
/// </summary>
public class CardDocumentService : ICardDocumentService
{
    private const string GroupName = "TẬP ĐOÀN INDEVCO";
    private const string ContactPhone = "02033.735.666";
    private const string FontFamily = "Times New Roman";

    private static readonly string[] NoticeLines =
    {
        "1. Thẻ mộ là giấy chứng nhận gia chủ có phần mộ đang đặt tại Nghĩa trang do Công ty quản lý. Thẻ này thay cho Hợp đồng quản lý mộ giữa C.N Công ty cổ phần tập đoàn INDEVCO - Xí nghiệp An Lạc và chủ mộ. Chủ mộ không được tự ý sửa chữa, tẩy xoá.",
        "2. Thẻ có giá trị sử dụng lâu dài, khi có sự thay đổi đơn vị quản lý nghĩa trang sẽ thông báo cho gia đình chủ mộ biết để giải quyết.",
        "3. Mọi trường hợp tranh chấp về phần mộ hoặc phát sinh khác, Công ty chỉ giải quyết khi chủ mộ xuất trình thẻ mộ.",
        "4. Khi Chủ mộ di chuyển phần mộ ra khỏi nghĩa trang phải nộp lại thẻ mộ cho Công ty.",
    };

    private static readonly Dictionary<string, string> GraveTypeLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SINGLE"] = "Đơn",
        ["DOUBLE"] = "Đôi",
        ["FAMILY"] = "Gia tộc",
        ["TRIPLE"] = "Ba",
    };

    private static bool _fontConfigured;
    private static readonly object FontLock = new();

    private readonly IOrganizationDbContextFactory _dbContextFactory;

    public CardDocumentService(IOrganizationDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<byte[]> RenderCardPdfAsync(long cardId, long companyId, string? watermarkOverride = null, CancellationToken ct = default)
    {
        await using var db = _dbContextFactory.CreateDbContext();

        var card = await db.Cards.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cardId && c.CompanyId == companyId, ct);
        if (card == null)
            throw new EntityNotFoundException("CARD_NOT_FOUND", "Không tìm thấy thẻ trong công ty đang chọn.");

        var grave = await db.Graves.AsNoTracking()
            .FirstOrDefaultAsync(g => g.GraveCode == card.GraveId, ct);
        var cemetery = grave == null ? null : await db.Cemeteries.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == grave.CemeteryId, ct);
        var company = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct);

        var occupants = grave == null ? new List<GraveOccupant>() : await db.GraveOccupants.AsNoTracking()
            .Where(o => o.GraveId == grave.Id)
            .OrderBy(o => o.Id)
            .Take(2)
            .ToListAsync(ct);

        Profile? owner = null;
        if (grave?.OwnerCustomerId != null)
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == grave.OwnerCustomerId, ct);
            if (customer != null)
                owner = await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == customer.ProfileId, ct);
        }

        EnsureFontResolver();

        var watermark = string.IsNullOrWhiteSpace(watermarkOverride) ? cemetery?.CardWatermarkCode : watermarkOverride;

        // Hoa văn dạng ẢNH TẢI LÊN (UPLOAD:{id}): nạp bytes + làm mờ sẵn để vẽ khi render (Render là đồng bộ).
        byte[]? watermarkImagePng = null;
        if (!string.IsNullOrWhiteSpace(watermark) && watermark!.StartsWith("UPLOAD:", System.StringComparison.OrdinalIgnoreCase)
            && long.TryParse(watermark.Substring("UPLOAD:".Length), out var wmId))
        {
            var wm = await db.CardWatermarks.AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == wmId && w.CompanyId == companyId && w.IsActive, ct);
            if (wm != null)
                watermarkImagePng = FaintImage(wm.ImageBytes, WatermarkImageIntensity);
            else
                watermark = null; // tham chiếu treo → bỏ hoa văn
        }

        var model = new CardModel(card, grave, cemetery, company?.Name ?? "", occupants, owner, watermark, watermarkImagePng);
        return Render(model);
    }

    // Độ đậm ảnh hoa văn tải lên: 0.30 để chìm nhưng vẫn thấy rõ (0.12 quá nhạt, ảnh sáng gần như mất).
    private const float WatermarkImageIntensity = 0.30f;

    /// <summary>Làm mờ ảnh hoa văn: giảm alpha xuống theo hệ số để chìm sau nội dung. Trả PNG.</summary>
    private static byte[] FaintImage(byte[] source, float intensity)
    {
        using var img = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(source);
        img.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                    row[x].A = (byte)(row[x].A * intensity);
            }
        });
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);
        return ms.ToArray();
    }

    private static void EnsureFontResolver()
    {
        if (_fontConfigured) return;
        lock (FontLock)
        {
            if (_fontConfigured) return;
            GlobalFontSettings.UseWindowsFontsUnderWindows = true;
            _fontConfigured = true;
        }
    }

    private sealed record CardModel(
        Card Card, Grave? Grave, Cemetery? Cemetery, string CompanyName,
        IReadOnlyList<GraveOccupant> Occupants, Profile? Owner, string? WatermarkCode, byte[]? WatermarkImagePng);

    // ── mm helpers ──
    private static double Mm(double v) => XUnit.FromMillimeter(v).Point;
    private const double PageW = 250, PageH = 176, FaceW = 125, Margin = 10;

    private byte[] Render(CardModel m)
    {
        using var doc = new PdfDocument();
        doc.Info.Title = $"The mo {m.Card.CardNumber}";

        var titleFont = new XFont(FontFamily, 15, XFontStyleEx.Bold);
        var sectionFont = new XFont(FontFamily, 13, XFontStyleEx.Bold);
        var bodyFont = new XFont(FontFamily, 10.5, XFontStyleEx.Regular);
        var boldFont = new XFont(FontFamily, 10.5, XFontStyleEx.Bold);
        var italicFont = new XFont(FontFamily, 10, XFontStyleEx.Italic);
        var smallFont = new XFont(FontFamily, 9.5, XFontStyleEx.Regular);

        // ── Trang 1: mặt ngoài (trái = lưu ý, phải = bìa) ──
        var p1 = doc.AddPage();
        p1.Width = XUnit.FromMillimeter(PageW);
        p1.Height = XUnit.FromMillimeter(PageH);
        using (var g = XGraphics.FromPdfPage(p1))
        {
            DrawWatermark(g, m, leftFace: true);
            DrawWatermark(g, m, leftFace: false);
            DrawFoldLine(g);
            DrawFaceBorder(g, leftFace: true);
            DrawFaceBorder(g, leftFace: false);
            DrawNoticeFace(g, m, sectionFont, bodyFont, boldFont, smallFont);
            DrawCoverFace(g, m, titleFont, sectionFont, bodyFont, boldFont, italicFont);
        }

        // ── Trang 2: mặt trong (trái = phần mộ, phải = chủ mộ) ──
        var p2 = doc.AddPage();
        p2.Width = XUnit.FromMillimeter(PageW);
        p2.Height = XUnit.FromMillimeter(PageH);
        using (var g = XGraphics.FromPdfPage(p2))
        {
            DrawWatermark(g, m, leftFace: true);
            DrawWatermark(g, m, leftFace: false);
            DrawFoldLine(g);
            DrawFaceBorder(g, leftFace: true);
            DrawFaceBorder(g, leftFace: false);
            DrawGraveFace(g, m, sectionFont, bodyFont, boldFont);
            DrawOwnerFace(g, m, sectionFont, bodyFont, boldFont, italicFont);
        }

        using var ms = new MemoryStream();
        doc.Save(ms);
        return ms.ToArray();
    }

    // ── Hoa văn chìm (watermark) — vẽ TRƯỚC nội dung, màu nhạt ──
    public const string WmLotusCode = "LOTUS";
    public const string WmFrameCode = "FRAME_CLASSIC";
    public const string WmDiagonalCode = "DIAGONAL_TEXT";
    private static readonly XColor FaintColor = XColor.FromArgb(232, 226, 214);

    private void DrawWatermark(XGraphics g, CardModel m, bool leftFace)
    {
        var code = m.WatermarkCode;
        if (string.IsNullOrWhiteSpace(code)) return;
        double x0 = leftFace ? 0 : FaceW;

        if (code.StartsWith("UPLOAD:", System.StringComparison.OrdinalIgnoreCase))
        {
            if (m.WatermarkImagePng != null) WmUploadedImage(g, x0, m.WatermarkImagePng);
            return;
        }

        switch (code.Trim().ToUpperInvariant())
        {
            case WmLotusCode: WmLotus(g, x0); break;
            case WmFrameCode: WmFrame(g, x0); break;
            case WmDiagonalCode: WmDiagonalText(g, x0, DiagonalWatermarkText(m)); break;
        }
    }

    private static void WmUploadedImage(XGraphics g, double x0, byte[] faintPng)
    {
        using var stream = new MemoryStream(faintPng);
        var xi = XImage.FromStream(stream);
        // Vừa khít vào giữa mặt, chừa lề, giữ tỉ lệ ảnh.
        double maxW = Mm(FaceW - 30), maxH = Mm(PageH - 60);
        double ratio = System.Math.Min(maxW / xi.PixelWidth, maxH / xi.PixelHeight);
        double w = xi.PixelWidth * ratio, h = xi.PixelHeight * ratio;
        double cx = Mm(x0 + FaceW / 2), cy = Mm(PageH / 2);
        g.DrawImage(xi, cx - w / 2, cy - h / 2, w, h);
    }

    private static string DiagonalWatermarkText(CardModel m)
    {
        var name = m.Cemetery?.Name;
        if (!string.IsNullOrWhiteSpace(name)) return name!.ToUpperInvariant();
        return string.IsNullOrWhiteSpace(m.CompanyName) ? "INDEVCO" : m.CompanyName.ToUpperInvariant();
    }

    private static void WmDiagonalText(XGraphics g, double x0, string text)
    {
        var font = new XFont(FontFamily, 30, XFontStyleEx.Bold);
        var brush = new XSolidBrush(FaintColor);
        double cx = Mm(x0 + FaceW / 2), cy = Mm(PageH / 2);
        var st = g.Save();
        g.RotateAtTransform(-32, new XPoint(cx, cy));
        for (int row = -3; row <= 3; row++)
            g.DrawString(text, font, brush, new XRect(Mm(x0 - 20), cy + row * Mm(26) - Mm(8), Mm(FaceW + 40), Mm(16)), XStringFormats.Center);
        g.Restore(st);
    }

    private static void WmLotus(XGraphics g, double x0)
    {
        var pen = new XPen(FaintColor, 0.8);
        double cx = Mm(x0 + FaceW / 2), cy = Mm(PageH / 2);
        double petalLen = Mm(42), petalWid = Mm(15);
        var st = g.Save();
        for (int i = 0; i < 8; i++)
        {
            var s2 = g.Save();
            g.RotateAtTransform(i * 45, new XPoint(cx, cy));
            g.DrawEllipse(pen, cx - petalWid / 2, cy - petalLen, petalWid, petalLen);
            g.Restore(s2);
        }
        for (int i = 0; i < 8; i++)
        {
            var s2 = g.Save();
            g.RotateAtTransform(i * 45 + 22.5, new XPoint(cx, cy));
            g.DrawEllipse(pen, cx - Mm(9) / 2, cy - Mm(26), Mm(9), Mm(26));
            g.Restore(s2);
        }
        g.DrawEllipse(pen, cx - Mm(7), cy - Mm(7), Mm(14), Mm(14));
        g.Restore(st);
    }

    private static void WmFrame(XGraphics g, double x0)
    {
        var pen = new XPen(FaintColor, 0.8);
        var thin = new XPen(FaintColor, 0.5);
        var brush = new XSolidBrush(FaintColor);
        double l = x0 + 12, t = 12, r = x0 + FaceW - 12, b = PageH - 12;
        g.DrawRectangle(pen, Mm(l), Mm(t), Mm(r - l), Mm(b - t));
        g.DrawRectangle(thin, Mm(l + 2), Mm(t + 2), Mm(r - l - 4), Mm(b - t - 4));
        void Corner(double cxm, double cym, bool left, bool top)
        {
            double s = 10;
            int start = left ? (top ? 180 : 90) : (top ? 270 : 0);
            g.DrawArc(pen, Mm(cxm - (left ? s : 0)), Mm(cym - (top ? s : 0)), Mm(s), Mm(s), start, 90);
            g.DrawArc(thin, Mm(cxm - (left ? s / 2 : 0)), Mm(cym - (top ? s / 2 : 0)), Mm(s / 2), Mm(s / 2), start, 90);
        }
        Corner(l, t, true, true); Corner(r, t, false, true); Corner(l, b, true, false); Corner(r, b, false, false);
        void Dot(double xm, double ym) => g.DrawEllipse(brush, Mm(xm - 1.5), Mm(ym - 1.5), Mm(3), Mm(3));
        Dot((l + r) / 2, t); Dot((l + r) / 2, b); Dot(l, (t + b) / 2); Dot(r, (t + b) / 2);
    }

    private static void DrawFoldLine(XGraphics g)
    {
        var pen = new XPen(XColors.LightGray, 0.4) { DashStyle = XDashStyle.Dash };
        g.DrawLine(pen, Mm(FaceW), Mm(4), Mm(FaceW), Mm(PageH - 4));
    }

    private static void DrawFaceBorder(XGraphics g, bool leftFace)
    {
        double x0 = leftFace ? 0 : FaceW;
        var pen = new XPen(XColors.Black, 0.8);
        g.DrawRectangle(pen, Mm(x0 + 6), Mm(6), Mm(FaceW - 12), Mm(PageH - 12));
        g.DrawRectangle(new XPen(XColors.Black, 0.4), Mm(x0 + 7.5), Mm(7.5), Mm(FaceW - 15), Mm(PageH - 15));
    }

    private static double FaceLeft(bool leftFace) => (leftFace ? 0 : FaceW) + Margin;
    private static double FaceContentWidth => FaceW - 2 * Margin;

    // ── Mặt bìa (phải, trang 1) ──
    private void DrawCoverFace(XGraphics g, CardModel m, XFont title, XFont section, XFont body, XFont bold, XFont italic)
    {
        double x = FaceLeft(false), w = FaceContentWidth;
        var rect = new XRect(Mm(x), Mm(18), Mm(w), Mm(10));
        g.DrawString(GroupName, body, XBrushes.Black, rect, XStringFormats.TopCenter);
        g.DrawString(m.CompanyName, bold, XBrushes.Black, new XRect(Mm(x), Mm(24), Mm(w), Mm(10)), XStringFormats.TopCenter);

        g.DrawString("THẺ QUẢN LÝ MỘ", title, XBrushes.Black, new XRect(Mm(x), Mm(66), Mm(w), Mm(12)), XStringFormats.TopCenter);
        var cemName = m.Cemetery?.Name ?? "—";
        DrawWrapped(g, $"TẠI NGHĨA TRANG {cemName.ToUpperInvariant()}", bold, XBrushes.Black, x, 80, w, 6, XStringFormats.TopCenter);

        g.DrawString($"SỐ THẺ: {m.Card.CardNumber ?? "—"}", bold, XBrushes.Black, new XRect(Mm(x), Mm(108), Mm(w), Mm(10)), XStringFormats.TopCenter);

        g.DrawString($"Cấp ngày {FormatLongDate(m.Card.CreatedAt)}", italic, XBrushes.Black, new XRect(Mm(x), Mm(PageH - 26), Mm(w), Mm(8)), XStringFormats.TopCenter);
    }

    // ── Mặt lưu ý (trái, trang 1) ──
    private void DrawNoticeFace(XGraphics g, CardModel m, XFont section, XFont body, XFont bold, XFont small)
    {
        double x = FaceLeft(true), w = FaceContentWidth;
        g.DrawString("QUÝ KHÁCH LƯU Ý:", section, XBrushes.Black, new XRect(Mm(x), Mm(16), Mm(w), Mm(8)), XStringFormats.TopCenter);

        double y = 28;
        foreach (var line in NoticeLines)
        {
            y = DrawWrapped(g, line, small, XBrushes.Black, x, y, w, 5.0, XStringFormats.TopLeft);
            y += 1.5;
        }

        y = Math.Max(y + 2, PageH - 42);
        g.DrawString("Địa chỉ liên hệ:", bold, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft);
        g.DrawString($"- {GroupName}", small, XBrushes.Black, new XRect(Mm(x + 24), Mm(y), Mm(w - 24), Mm(6)), XStringFormats.TopLeft);
        g.DrawString($"- {m.CompanyName}", small, XBrushes.Black, new XRect(Mm(x + 24), Mm(y + 5), Mm(w - 24), Mm(6)), XStringFormats.TopLeft);
        g.DrawString($"Điện thoại: {ContactPhone}", bold, XBrushes.Black, new XRect(Mm(x), Mm(PageH - 20), Mm(w), Mm(6)), XStringFormats.TopCenter);
    }

    // ── Mặt phần mộ (trái, trang 2) ──
    private void DrawGraveFace(XGraphics g, CardModel m, XFont section, XFont body, XFont bold)
    {
        double x = FaceLeft(true), w = FaceContentWidth;
        g.DrawString("THÔNG TIN VỀ PHẦN MỘ", section, XBrushes.Black, new XRect(Mm(x), Mm(14), Mm(w), Mm(8)), XStringFormats.TopCenter);

        double y = 28;
        for (int i = 0; i < 2; i++)
        {
            var o = i < m.Occupants.Count ? m.Occupants[i] : null;
            g.DrawString($"{i + 1}/ Phần mộ: {o?.FullName ?? "."}", body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft); y += 6;
            g.DrawString($"Năm sinh: {YearOrDot(o?.Dob)}", body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft); y += 6;
            g.DrawString($"Mất ngày {o?.DeathDateLunar ?? "."} (Âm lịch)", body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft); y += 6;
            g.DrawString($"Nơi mất: {Dot(o?.Hometown)}", body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft); y += 8;
        }

        var cemName = m.Cemetery?.Name ?? "—";
        y = DrawWrapped(g, $"Phần mộ đặt tại nghĩa trang {cemName}", bold, XBrushes.Black, x, y + 2, w, 6, XStringFormats.TopLeft) + 3;

        g.DrawString($"Thuộc khu: {Dot(m.Grave?.Zone)}", body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft); y += 6;
        g.DrawString($"Loại mộ: {GraveTypeLabel(m.Grave?.GraveType)}", body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft); y += 6;
        g.DrawString($"Số: {Dot(m.Grave?.PlotNumber)}", body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(w), Mm(6)), XStringFormats.TopLeft);

        g.DrawString($"Cấp ngày {FormatShortDate(m.Card.CreatedAt)}", body, XBrushes.Black, new XRect(Mm(x), Mm(PageH - 20), Mm(w), Mm(6)), XStringFormats.TopCenter);
    }

    // ── Mặt chủ mộ (phải, trang 2) ──
    private void DrawOwnerFace(XGraphics g, CardModel m, XFont section, XFont body, XFont bold, XFont italic)
    {
        double x = FaceLeft(false), w = FaceContentWidth;
        var o = m.Owner;
        g.DrawString("THÔNG TIN VỀ CHỦ MỘ", section, XBrushes.Black, new XRect(Mm(x), Mm(14), Mm(w), Mm(8)), XStringFormats.TopCenter);

        double y = 28;
        void Line(string label, string? value)
        {
            g.DrawString(label, body, XBrushes.Black, new XRect(Mm(x), Mm(y), Mm(34), Mm(6)), XStringFormats.TopLeft);
            g.DrawString(Dot(value), bold, XBrushes.Black, new XRect(Mm(x + 34), Mm(y), Mm(w - 34), Mm(6)), XStringFormats.TopLeft);
            y += 6;
        }
        Line("Họ và tên:", o?.FullName);
        Line("Giới tính:", GenderLabel(o?.Gender));
        Line("Sinh năm:", YearOrDot(o?.Dob));
        Line("Số CMND:", o?.Cccd);
        Line("Cấp ngày:", o?.CccdIssueDate == null ? null : FormatShortDate(o.CccdIssueDate.Value));
        Line("Nơi cấp:", o?.CccdIssuePlace);
        y = DrawLabelWrapped(g, "Địa chỉ liên lạc:", o?.ContactAddress ?? o?.PermanentAddress, body, bold, x, y, w, 34) ;
        Line("Điện thoại:", o?.Phone);

        // Khối ký tên: chủ mộ (ký) — P.Giám đốc (dấu + chữ ký để trống, ký tay).
        double sy = PageH - 42;
        g.DrawString("CHỦ MỘ", bold, XBrushes.Black, new XRect(Mm(x), Mm(sy), Mm(w / 2), Mm(6)), XStringFormats.TopCenter);
        g.DrawString("(Ký, ghi rõ họ tên)", italic, XBrushes.Black, new XRect(Mm(x), Mm(sy + 5), Mm(w / 2), Mm(5)), XStringFormats.TopCenter);
        g.DrawString($"{GroupName.Replace("TẬP ĐOÀN ", "INDEVCO - ")}", bold, XBrushes.Black, new XRect(Mm(x + w / 2), Mm(sy), Mm(w / 2), Mm(6)), XStringFormats.TopCenter);
        g.DrawString("P. GIÁM ĐỐC", bold, XBrushes.Black, new XRect(Mm(x + w / 2), Mm(sy + 5), Mm(w / 2), Mm(6)), XStringFormats.TopCenter);
    }

    // ── text helpers ──
    private static double DrawWrapped(XGraphics g, string text, XFont font, XBrush brush, double xMm, double yMm, double wMm, double lineHeightMm, XStringFormat fmt)
    {
        var words = text.Split(' ');
        var line = "";
        double y = yMm;
        foreach (var word in words)
        {
            var probe = line.Length == 0 ? word : line + " " + word;
            if (g.MeasureString(probe, font).Width > Mm(wMm) && line.Length > 0)
            {
                g.DrawString(line, font, brush, new XRect(Mm(xMm), Mm(y), Mm(wMm), Mm(lineHeightMm)), fmt);
                y += lineHeightMm;
                line = word;
            }
            else
            {
                line = probe;
            }
        }
        if (line.Length > 0)
        {
            g.DrawString(line, font, brush, new XRect(Mm(xMm), Mm(y), Mm(wMm), Mm(lineHeightMm)), fmt);
            y += lineHeightMm;
        }
        return y;
    }

    private static double DrawLabelWrapped(XGraphics g, string label, string? value, XFont labelFont, XFont valueFont, double xMm, double yMm, double wMm, double labelWMm)
    {
        g.DrawString(label, labelFont, XBrushes.Black, new XRect(Mm(xMm), Mm(yMm), Mm(labelWMm), Mm(6)), XStringFormats.TopLeft);
        var end = DrawWrapped(g, Dot(value), valueFont, XBrushes.Black, xMm + labelWMm, yMm, wMm - labelWMm, 5.5, XStringFormats.TopLeft);
        return Math.Max(yMm + 6, end);
    }

    private static string Dot(string? v) => string.IsNullOrWhiteSpace(v) ? "." : v.Trim();
    private static string YearOrDot(DateTime? d) => d.HasValue ? d.Value.Year.ToString() : ".";
    private static string GraveTypeLabel(string? code)
        => string.IsNullOrWhiteSpace(code) ? "." : (GraveTypeLabels.TryGetValue(code, out var l) ? l : code);
    private static string GenderLabel(string? g)
        => string.IsNullOrWhiteSpace(g) ? "." : g.Trim().ToUpperInvariant() switch
        {
            "M" or "MALE" or "NAM" => "Nam",
            "F" or "FEMALE" or "NU" or "NỮ" => "Nữ",
            _ => g!,
        };
    private static string FormatLongDate(DateTime d) => $"{d:dd} tháng {d:MM} năm {d:yyyy}";
    private static string FormatShortDate(DateTime d) => d.ToString("dd/MM/yyyy");
}
