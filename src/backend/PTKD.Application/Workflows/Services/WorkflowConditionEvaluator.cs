using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using PTKD.Domain.Entities;

namespace PTKD.Application.Workflows.Services;

/// <summary>Một điều kiện cần so khớp (tách khỏi entity để test được và để dùng cho snapshot).</summary>
public sealed record ConditionCheck(string FieldCode, string Operator, string Value);

/// <summary>
/// Đánh giá điều kiện của một phiên bản quy trình trên payload của hồ sơ.
///
/// Nguyên tắc an toàn (theo tài liệu quản trị):
///  - CHỈ so khớp trường / toán tử / giá trị đã khai báo. Không có SQL, không có biểu thức tự do.
///  - Nhiều điều kiện trên cùng một phiên bản = VÀ (tất cả phải đúng).
///  - Thiếu trường trong payload hoặc dữ liệu không ép được kiểu → coi là KHÔNG KHỚP.
///    Thà không áp dụng quy trình còn hơn áp dụng nhầm vì so sánh rác.
/// </summary>
public static class WorkflowConditionEvaluator
{
    public const string OpEq = "EQ";
    public const string OpNeq = "NEQ";
    public const string OpGt = "GT";
    public const string OpLt = "LT";
    public const string OpGte = "GTE";
    public const string OpLte = "LTE";
    public const string OpIn = "IN";
    public const string OpContains = "CONTAINS";

    public static readonly string[] SupportedOperators =
        [OpEq, OpNeq, OpGt, OpLt, OpGte, OpLte, OpIn, OpContains];

    /// <summary>Toán tử hợp lệ theo kiểu dữ liệu của trường.</summary>
    public static string[] OperatorsForDataType(string dataType) => dataType switch
    {
        WorkflowConditionField.TypeNumber => [OpEq, OpNeq, OpGt, OpLt, OpGte, OpLte, OpIn],
        WorkflowConditionField.TypeDate => [OpEq, OpNeq, OpGt, OpLt, OpGte, OpLte],
        WorkflowConditionField.TypeBoolean => [OpEq, OpNeq],
        _ => [OpEq, OpNeq, OpIn, OpContains],
    };

    /// <summary>Không có điều kiện nào = luôn áp dụng.</summary>
    public static bool Matches(IReadOnlyCollection<ConditionCheck> conditions, string? payloadJson)
    {
        if (conditions.Count == 0) return true;

        Dictionary<string, JsonElement> payload;
        try
        {
            payload = ParsePayload(payloadJson);
        }
        catch (JsonException)
        {
            return false; // payload hỏng → không khớp (an toàn)
        }

        return conditions.All(c => MatchesOne(c, payload));
    }

    private static Dictionary<string, JsonElement> ParsePayload(string? payloadJson)
    {
        var result = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(payloadJson)) return result;

        using var doc = JsonDocument.Parse(payloadJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object) return result;

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            // Clone vì JsonDocument bị giải phóng khi ra khỏi using.
            result[prop.Name] = prop.Value.Clone();
        }
        return result;
    }

    private static bool MatchesOne(ConditionCheck condition, Dictionary<string, JsonElement> payload)
    {
        if (!payload.TryGetValue(condition.FieldCode, out var element))
            return false; // payload không có trường này → không khớp

        // So sánh SỐ khi cả hai bên đều là số; ngược lại so sánh chuỗi.
        var actualNumber = TryGetNumber(element);
        var expectedNumber = TryParseNumber(condition.Value);

        if (actualNumber.HasValue && expectedNumber.HasValue)
            return CompareNumber(actualNumber.Value, condition.Operator, expectedNumber.Value, condition.Value);

        var actualText = GetText(element);
        if (actualText is null) return false;

        return CompareText(actualText, condition.Operator, condition.Value);
    }

    private static bool CompareNumber(decimal actual, string op, decimal expected, string rawExpected) => op switch
    {
        OpEq => actual == expected,
        OpNeq => actual != expected,
        OpGt => actual > expected,
        OpLt => actual < expected,
        OpGte => actual >= expected,
        OpLte => actual <= expected,
        OpIn => SplitList(rawExpected)
            .Select(TryParseNumber)
            .Any(n => n.HasValue && n.Value == actual),
        _ => false, // CONTAINS không có nghĩa với số
    };

    private static bool CompareText(string actual, string op, string expected) => op switch
    {
        OpEq => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        OpNeq => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        OpIn => SplitList(expected).Any(v => string.Equals(actual, v, StringComparison.OrdinalIgnoreCase)),
        OpContains => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
        // So sánh lớn/nhỏ trên chuỗi dễ gây hiểu nhầm (vd "10" < "9") → không hỗ trợ.
        _ => false,
    };

    private static IEnumerable<string> SplitList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static decimal? TryGetNumber(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number => element.TryGetDecimal(out var d) ? d : null,
        JsonValueKind.String => TryParseNumber(element.GetString()),
        _ => null,
    };

    private static decimal? TryParseNumber(string? value) =>
        decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static string? GetText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null,
    };
}
