using Pathfinding;
using UnityEngine;

public static class NodeGraphUtility
{
    public static GraphNode GetNodeAtPosition(Vector3 worldPosition)
    {
        // Get the active GridGraph
        GridGraph gridGraph = AstarPath.active.data.gridGraph;

        if (gridGraph == null)
        {
            Debug.LogError("No GridGraph found in AstarPath!");
            return null;
        }

        // Use GetNearest to find the closest node to the given world position
        NNInfoInternal nearestNodeInfo = gridGraph.GetNearest(worldPosition);
        return nearestNodeInfo.node;
    }
}
