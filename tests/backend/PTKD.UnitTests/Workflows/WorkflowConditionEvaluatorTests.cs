using System.Collections.Generic;
using PTKD.Application.Workflows.Services;
using PTKD.Domain.Entities;
using Xunit;

namespace PTKD.UnitTests.Workflows;

/// <summary>
/// Bộ đánh giá điều kiện quyết định quy trình phê duyệt nào áp dụng cho một hồ sơ, nên sai ở đây
/// nghĩa là hồ sơ đi nhầm cấp duyệt — hoặc thoát duyệt. Vì thế kiểm khá kỹ các ca biên.
/// </summary>
public class WorkflowConditionEvaluatorTests
{
    private const string Payload = """{"TotalAmount": 60000000, "ReasonCode": "LOST", "CotCount": 2}""";

    private static List<ConditionCheck> Cond(string field, string op, string value) =>
        [new ConditionCheck(field, op, value)];

    [Fact]
    public void NoConditions_AlwaysMatches()
    {
        Assert.True(WorkflowConditionEvaluator.Matches([], Payload));
        Assert.True(WorkflowConditionEvaluator.Matches([], null));
    }

    [Theory]
    [InlineData("GT", "50000000", true)]
    [InlineData("GT", "60000000", false)]
    [InlineData("GTE", "60000000", true)]
    [InlineData("LT", "70000000", true)]
    [InlineData("LTE", "60000000", true)]
    [InlineData("EQ", "60000000", true)]
    [InlineData("NEQ", "60000000", false)]
    public void NumberComparisons_Work(string op, string value, bool expected)
    {
        Assert.Equal(expected, WorkflowConditionEvaluator.Matches(Cond("TotalAmount", op, value), Payload));
    }

    [Fact]
    public void Number_In_MatchesAnyOfList()
    {
        Assert.True(WorkflowConditionEvaluator.Matches(Cond("CotCount", "IN", "1, 2, 3"), Payload));
        Assert.False(WorkflowConditionEvaluator.Matches(Cond("CotCount", "IN", "4,5"), Payload));
    }

    [Theory]
    [InlineData("EQ", "LOST", true)]
    [InlineData("EQ", "lost", true)]      // không phân biệt hoa thường
    [InlineData("NEQ", "LOST", false)]
    [InlineData("CONTAINS", "OS", true)]
    [InlineData("IN", "LOST,DAMAGED", true)]
    [InlineData("IN", "DAMAGED", false)]
    public void TextComparisons_Work(string op, string value, bool expected)
    {
        Assert.Equal(expected, WorkflowConditionEvaluator.Matches(Cond("ReasonCode", op, value), Payload));
    }

    [Fact]
    public void MultipleConditions_AreAndedTogether()
    {
        List<ConditionCheck> both =
        [
            new ConditionCheck("TotalAmount", "GT", "50000000"),
            new ConditionCheck("ReasonCode", "EQ", "LOST"),
        ];
        Assert.True(WorkflowConditionEvaluator.Matches(both, Payload));

        List<ConditionCheck> oneFails =
        [
            new ConditionCheck("TotalAmount", "GT", "50000000"),
            new ConditionCheck("ReasonCode", "EQ", "DAMAGED"),
        ];
        Assert.False(WorkflowConditionEvaluator.Matches(oneFails, Payload));
    }

    [Fact]
    public void MissingField_DoesNotMatch()
    {
        // Thà không áp dụng quy trình còn hơn áp dụng nhầm vì so sánh trên dữ liệu không có.
        Assert.False(WorkflowConditionEvaluator.Matches(Cond("KhongCoTruongNay", "EQ", "1"), Payload));
    }

    [Fact]
    public void MalformedOrEmptyPayload_DoesNotMatch()
    {
        Assert.False(WorkflowConditionEvaluator.Matches(Cond("TotalAmount", "GT", "1"), "{ khong phai json"));
        Assert.False(WorkflowConditionEvaluator.Matches(Cond("TotalAmount", "GT", "1"), null));
        Assert.False(WorkflowConditionEvaluator.Matches(Cond("TotalAmount", "GT", "1"), "[]"));
    }

    [Fact]
    public void NonNumericExpectedValue_OnNumberField_DoesNotMatch()
    {
        // Giá trị so sánh gõ sai kiểu → không khớp, thay vì ném lỗi làm hỏng việc tạo hồ sơ.
        Assert.False(WorkflowConditionEvaluator.Matches(Cond("TotalAmount", "GT", "abc"), Payload));
    }

    [Fact]
    public void GreaterThan_OnTextField_DoesNotMatch()
    {
        // So sánh lớn/nhỏ trên chuỗi dễ gây hiểu nhầm nên cố ý không hỗ trợ.
        Assert.False(WorkflowConditionEvaluator.Matches(Cond("ReasonCode", "GT", "A"), Payload));
    }

    [Fact]
    public void NumberStoredAsString_StillComparesNumerically()
    {
        const string payload = """{"TotalAmount": "60000000"}""";
        Assert.True(WorkflowConditionEvaluator.Matches(Cond("TotalAmount", "GT", "50000000"), payload));
    }

    [Fact]
    public void OperatorsForDataType_RestrictsByType()
    {
        Assert.Contains("GT", WorkflowConditionEvaluator.OperatorsForDataType(WorkflowConditionField.TypeNumber));
        Assert.DoesNotContain("CONTAINS", WorkflowConditionEvaluator.OperatorsForDataType(WorkflowConditionField.TypeNumber));

        Assert.Contains("CONTAINS", WorkflowConditionEvaluator.OperatorsForDataType(WorkflowConditionField.TypeText));
        Assert.DoesNotContain("GT", WorkflowConditionEvaluator.OperatorsForDataType(WorkflowConditionField.TypeText));

        Assert.Equal(2, WorkflowConditionEvaluator.OperatorsForDataType(WorkflowConditionField.TypeBoolean).Length);
    }
}
