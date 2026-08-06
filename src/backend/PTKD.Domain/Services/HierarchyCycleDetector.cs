using System.Collections.Generic;
using System.Linq;

namespace PTKD.Domain.Services;

public static class HierarchyCycleDetector
{
    public static bool HasCycle(long entityId, long? proposedParentId, IDictionary<long, long?> allNodes)
    {
        if (!proposedParentId.HasValue)
            return false;

        if (proposedParentId.Value == entityId)
            return true;

        var currentParentId = proposedParentId;
        while (currentParentId.HasValue)
        {
            if (currentParentId.Value == entityId)
                return true;

            if (!allNodes.TryGetValue(currentParentId.Value, out var nextParentId))
                break;

            currentParentId = nextParentId;
        }

        return false;
    }
}
