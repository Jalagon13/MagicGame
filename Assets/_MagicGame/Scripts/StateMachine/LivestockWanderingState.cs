using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class LivestockWanderingState : BaseState<LivestockStateMachine.LivestockState>
{
	private readonly LivestockStateMachine _ctx;
	private bool _reachedDestination;
	private readonly int _maxCalculateAttempts = 50;

	public LivestockWanderingState(LivestockStateMachine.LivestockState key, StateMachine<LivestockStateMachine.LivestockState> context) : base(key, context)
	{
		_ctx = Context as LivestockStateMachine;
	}

	public override void EnterState()
	{
		_ctx.Agent.maxSpeed = _ctx.WanderSpeed;
		_ctx.IsMoving = true;
		_ctx.OnDirectionChange(_ctx.LookDirection);
		_reachedDestination = false;

		// Attempt to find a valid destination
		if (!TryFindValidDestination())
		{
			Debug.LogWarning("Could not find a valid destination. Transitioning to Idle state.");
			_ctx.TransitionToState(LivestockStateMachine.LivestockState.Idle);
		}
	}

	public override void ExitState() { }

	public override LivestockStateMachine.LivestockState GetNextState()
	{
		return _reachedDestination ? LivestockStateMachine.LivestockState.Idle : StateKey;
	}

	public override void FixedUpdate()
	{
		if (_ctx.Agent.reachedDestination)
		{
			_reachedDestination = true;
		}
	}

	/// <summary>
	/// Tries to find a valid wander destination within the maximum number of attempts.
	/// </summary>
	private bool TryFindValidDestination()
	{
		for (int attempt = 0; attempt < _maxCalculateAttempts; attempt++)
		{
			Vector3 destination = CalculateWanderDestination();

			if (_ctx.DestinationValid(destination))
			{
				_ctx.Agent.destination = destination;
				return true; // Successfully found a valid destination
			}
		}

		// Failed to find a valid destination after all attempts
		return false;
	}

	/// <summary>
	/// Calculates a random wander destination by finding a walkable node within range.
	/// </summary>
	private Vector3 CalculateWanderDestination()
	{
		var startNode = _ctx.NpcGridGraph.GetNearest(_ctx.transform.position, NNConstraint.Default).node;

		// Attempt to find a list of nodes within range
		for (int attempt = 0; attempt < _maxCalculateAttempts; attempt++)
		{
			var nodes = PathUtilities.BFS(startNode, _ctx.MaxWanderNodeDistance);

			if (nodes.Count > 0)
			{
				// If nodes are found, select a random point and return it
				return PathUtilities.GetPointsOnNodes(nodes, 1)[0];
			}

			Debug.LogWarning($"Attempt {attempt + 1}/{_maxCalculateAttempts}: No valid nodes found.");
		}

		// Return the NPC's current position as a fallback (or any other behavior you prefer)
		Debug.LogWarning("Failed to find valid nodes for wander destination after all attempts.");
		return _ctx.transform.position;
	}
}