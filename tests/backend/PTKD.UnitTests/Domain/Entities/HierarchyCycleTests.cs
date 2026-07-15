using System.Collections.Generic;
using PTKD.Domain.Services;
using Xunit;

namespace PTKD.UnitTests.Domain.Entities;

public class HierarchyCycleTests
{
    [Fact]
    public void HasCycle_NullParentId_ReturnsFalse()
    {
        var allNodes = new Dictionary<long, long?>();
        bool result = HierarchyCycleDetector.HasCycle(1, null, allNodes);
        Assert.False(result);
    }

    [Fact]
    public void HasCycle_DirectSelfParent_ReturnsTrue()
    {
        var allNodes = new Dictionary<long, long?>();
        bool result = HierarchyCycleDetector.HasCycle(1, 1, allNodes);
        Assert.True(result);
    }

    [Fact]
    public void HasCycle_GrandparentIsSelf_ReturnsTrue()
    {
        var allNodes = new Dictionary<long, long?>
        {
            { 2, 1 } // Node 2's parent is 1
        };
        
        // We are checking if Node 1 can have Node 2 as its parent
        bool result = HierarchyCycleDetector.HasCycle(1, 2, allNodes);
        Assert.True(result);
    }

    [Fact]
    public void HasCycle_DeepCycle_ReturnsTrue()
    {
        var allNodes = new Dictionary<long, long?>
        {
            { 2, 3 },
            { 3, 4 },
            { 4, 1 } // Node 4's parent is 1
        };
        
        // Node 1 wants Node 2 as parent -> 2 -> 3 -> 4 -> 1 (Cycle)
        bool result = HierarchyCycleDetector.HasCycle(1, 2, allNodes);
        Assert.True(result);
    }

    [Fact]
    public void HasCycle_NoCycle_ReturnsFalse()
    {
        var allNodes = new Dictionary<long, long?>
        {
            { 2, 3 },
            { 3, 4 },
            { 4, null }
        };
        
        // Node 1 wants Node 2 as parent -> 2 -> 3 -> 4 -> null
        bool result = HierarchyCycleDetector.HasCycle(1, 2, allNodes);
        Assert.False(result);
    }
}
