using System.Collections.Generic;
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
	
	}

	private bool TryFindValidDestination()
	{
		for (int attempt = 0; attempt < _maxCalculateAttempts; attempt++)
		{
			Vector3 destination = CalculateWanderDestination();

			if (_ctx.DestinationValid(destination))
			{
				return true; // Successfully found a valid destination
			}
		}

		// Failed to find a valid destination after all attempts
		return false;
	}

	private Vector3 CalculateWanderDestination()
	{

		// Return the NPC's current position as a fallback (or any other behavior you prefer)
		Debug.LogWarning("Failed to find valid nodes for wander destination after all attempts.");
		return _ctx.transform.position;
	}
}