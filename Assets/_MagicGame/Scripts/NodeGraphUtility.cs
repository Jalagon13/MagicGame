using System;
using Pathfinding;
using UnityEngine;

public static class NodeGraphUtility
{
	public static GridGraph GetGridGraph(EnvironmentID environment)
	{
		// Get the A* Pathfinding Graphs instance
		AstarData astarData = AstarPath.active.data;

		// Find the GridGraph for the specified environment
		string environmentGraphName = environment.ToString();

		foreach (NavGraph graph in astarData.graphs)
		{
			if (graph is GridGraph gridGraph && gridGraph.name == environmentGraphName)
			{
				return gridGraph; // Return the matching GridGraph
			}
		}

		// If no matching GridGraph is found, log a warning and return null
		Debug.LogWarning($"No GridGraph found for environment {environment}!");
		return null;
	}

	public static void TryToCreateGridGraph(EnvironmentID environment)
	{
		// If a grid graph for this environment exists, don't do anything
		if (GridGraphExists(environment)) 
		{
			Debug.Log($"GridGraph for environment {environment} already exists.");
			return;
		}
		
		// Get the A* Pathfinding Graphs instance
		AstarData astarData = AstarPath.active.data;

		// Get the first active GridGraph
		GridGraph activeGridGraph = astarData.graphs[0] as GridGraph;

		if (activeGridGraph == null)
		{
			Debug.LogError("No active GridGraph found to duplicate.");
			return;
		}

		// Create a new GridGraph
		GridGraph newGridGraph = astarData.AddGraph(typeof(GridGraph)) as GridGraph;

		if (newGridGraph == null)
		{
			Debug.LogError("Failed to create a new GridGraph.");
			return;
		}

		// Copy settings from the active GridGraph
		CopyGridGraphSettings(activeGridGraph, newGridGraph);

		newGridGraph.name = environment.ToString();

		// Scan the new graph
		AstarPath.active.Scan(newGridGraph);

		Debug.Log($"GridGraph duplicated and scanned successfully for {environment.ToString()} environment");
	}
	
	private static void CopyGridGraphSettings(GridGraph source, GridGraph target)
	{
		// Copy settings from the source grid to the target grid
		target.SetDimensions(source.width, source.depth, source.nodeSize);
		target.center = source.center;
		target.rotation = source.rotation;

		target.collision = source.collision; // Copy collision settings
		target.maxSlope = source.maxSlope;
		target.erodeIterations = source.erodeIterations;
		target.penaltyPosition = source.penaltyPosition;
		target.penaltyPositionFactor = source.penaltyPositionFactor;
		target.penaltyAngle = source.penaltyAngle;
		target.penaltyAngleFactor = source.penaltyAngleFactor;

		// Additional settings you want to copy can be added here
	}

	private static bool GridGraphExists(EnvironmentID environment)
	{
		// Get the A* Pathfinding Graphs instance
		AstarData astarData = AstarPath.active.data;

		// Name of the environment's GridGraph
		string environmentGraphName = environment.ToString();

		// Iterate through all existing graphs
		foreach (NavGraph graph in astarData.graphs)
		{
			// Skip null graphs
			if (graph == null) continue;

			// Check if the graph name matches the environment's name
			if (graph is GridGraph gridGraph && gridGraph.name == environmentGraphName)
			{
				return true; // A GridGraph with this environment's name already exists
			}
		}

		// No matching GridGraph was found
		return false;
	}
	
	public static void SetNodeToWalkable(Vector2 centerNodePosition, EnvironmentID environment, bool isWalkable)
	{
		// var node = GetNodeAtPosition(centerNodePosition, environment);
		// node.Walkable = isWalkable;
	}

	private static GraphNode GetNodeAtPosition(Vector3 worldPosition, EnvironmentID environment)
	{
		// Get the A* Pathfinding Graphs instance
		AstarData astarData = AstarPath.active.data;

		// Find the GridGraph for the specified environment
		string environmentGraphName = environment.ToString();
		GridGraph targetGridGraph = null;

		foreach (NavGraph graph in astarData.graphs)
		{
			if (graph is GridGraph gridGraph && gridGraph.name == environmentGraphName)
			{
				targetGridGraph = gridGraph;
				break;
			}
		}

		// If no matching GridGraph is found, log an error and return null
		if (targetGridGraph == null)
		{
			Debug.LogError($"No GridGraph found for environment {environment}!");
			return null;
		}

		// Use GetNearest to find the closest node to the given world position
		NNInfoInternal nearestNodeInfo = targetGridGraph.GetNearest(worldPosition);
		return nearestNodeInfo.node;
	}
}
