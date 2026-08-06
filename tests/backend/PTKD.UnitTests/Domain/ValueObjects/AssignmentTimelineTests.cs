using System;
using PTKD.Domain.ValueObjects;
using Xunit;

namespace PTKD.UnitTests.Domain.ValueObjects;

public class AssignmentTimelineTests
{
    [Fact]
    public void Create_WithValidDates_CreatesSuccessfully()
    {
        var from = new DateTime(2023, 1, 1);
        var to = new DateTime(2023, 12, 31);
        
        var timeline = AssignmentTimeline.Create(from, to);
        
        Assert.Equal(from, timeline.EffectiveFrom);
        Assert.Equal(to, timeline.EffectiveTo);
    }

    [Fact]
    public void Create_WithNullEffectiveTo_CreatesSuccessfully()
    {
        var from = new DateTime(2023, 1, 1);
        
        var timeline = AssignmentTimeline.Create(from, null);
        
        Assert.Equal(from, timeline.EffectiveFrom);
        Assert.Null(timeline.EffectiveTo);
    }

    [Fact]
    public void Create_WithToBeforeFrom_ThrowsArgumentException()
    {
        var from = new DateTime(2023, 12, 31);
        var to = new DateTime(2023, 1, 1);
        
        Assert.Throws<ArgumentException>(() => AssignmentTimeline.Create(from, to));
    }
    
    [Fact]
    public void Create_WithToEqualFrom_ThrowsArgumentException()
    {
        var from = new DateTime(2023, 1, 1);
        var to = new DateTime(2023, 1, 1);
        
        Assert.Throws<ArgumentException>(() => AssignmentTimeline.Create(from, to));
    }

    [Theory]
    [InlineData("2023-01-01", "2023-12-31", "2023-06-01", true)]
    [InlineData("2023-01-01", "2023-12-31", "2023-01-01", true)]
    [InlineData("2023-01-01", "2023-12-31", "2022-12-31", false)]
    [InlineData("2023-01-01", "2023-12-31", "2023-12-31", false)] // Half-open interval
    public void Contains_Date_ReturnsExpectedResult(string fromStr, string toStr, string dateStr, bool expected)
    {
        var from = DateTime.Parse(fromStr);
        var to = DateTime.Parse(toStr);
        var date = DateTime.Parse(dateStr);
        
        var timeline = AssignmentTimeline.Create(from, to);
        
        Assert.Equal(expected, timeline.Contains(date));
    }

    [Fact]
    public void Contains_Date_NullTo_ReturnsExpectedResult()
    {
        var from = new DateTime(2023, 1, 1);
        var timeline = AssignmentTimeline.Create(from, null);
        
        Assert.False(timeline.Contains(new DateTime(2022, 12, 31)));
        Assert.True(timeline.Contains(new DateTime(2023, 1, 1)));
        Assert.True(timeline.Contains(new DateTime(2099, 12, 31)));
    }

    [Fact]
    public void Overlaps_WithOverlap_ReturnsTrue()
    {
        var t1 = AssignmentTimeline.Create(new DateTime(2023, 1, 1), new DateTime(2023, 6, 1));
        var t2 = AssignmentTimeline.Create(new DateTime(2023, 5, 1), new DateTime(2023, 12, 1));
        
        Assert.True(t1.Overlaps(t2));
        Assert.True(t2.Overlaps(t1));
    }

    [Fact]
    public void Overlaps_NoOverlap_ReturnsFalse()
    {
        var t1 = AssignmentTimeline.Create(new DateTime(2023, 1, 1), new DateTime(2023, 5, 1));
        var t2 = AssignmentTimeline.Create(new DateTime(2023, 5, 1), new DateTime(2023, 12, 1));
        
        Assert.False(t1.Overlaps(t2));
        Assert.False(t2.Overlaps(t1));
    }

    [Fact]
    public void Overlaps_OneInfinite_ReturnsExpected()
    {
        var t1 = AssignmentTimeline.Create(new DateTime(2023, 1, 1), new DateTime(2023, 5, 1));
        var t2 = AssignmentTimeline.Create(new DateTime(2023, 4, 1), null);
        var t3 = AssignmentTimeline.Create(new DateTime(2023, 5, 1), null);
        
        Assert.True(t1.Overlaps(t2));
        Assert.False(t1.Overlaps(t3));
    }

    [Fact]
    public void Overlaps_BothInfinite_ReturnsTrue()
    {
        var t1 = AssignmentTimeline.Create(new DateTime(2023, 1, 1), null);
        var t2 = AssignmentTimeline.Create(new DateTime(2023, 5, 1), null);
        
        Assert.True(t1.Overlaps(t2));
    }
}
