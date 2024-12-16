using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class LivestockWanderingState : BaseState<LivestockStateMachine.LivestockState>
{
    private readonly LivestockStateMachine _ctx;
    private bool _reachedDestination, _validDestinationFound;
    private readonly int _maxCalculateAttempts = 25;

    public LivestockWanderingState(LivestockStateMachine.LivestockState key, StateMachine<LivestockStateMachine.LivestockState> context) : base(key, context)
    {
        _ctx = Context as LivestockStateMachine;
    }

    public override void EnterState()
    {
        // Debug.Log($"[Client {_ctx.NetworkManager.LocalClientId}] Entering wandering");
		
        _ctx.Agent.maxSpeed = _ctx.WanderSpeed;
        _ctx.IsMoving = true;
        _ctx.OnDirectionChange(_ctx.LookDirection);
        _reachedDestination = false;
        _validDestinationFound = false;
		
        // Try to find a valid spawn spot and move to it
        int calculateAttempts = 0;
        while(calculateAttempts < _maxCalculateAttempts)
        {
            Vector3 destination = CalculateWanderDestination(); 	
            if(_ctx.DestinationValid(destination))
            {
                _ctx.Agent.destination = destination;
                _validDestinationFound = true;
                break;
            }
            calculateAttempts++;
        }
		
        // If it could not find a spot to move, then transition back to idle state
        if(!_validDestinationFound)
        {
            _ctx.TransitionToState(LivestockStateMachine.LivestockState.Idle);
        }
    }

    public override void ExitState()
    {
    }
	
    public override LivestockStateMachine.LivestockState GetNextState()
    {
        // when target reaches its destination, return to idle state
        return _reachedDestination ? LivestockStateMachine.LivestockState.Idle : StateKey;
    }

    public override void FixedUpdate()
    {
        if(_ctx.Agent.reachedDestination && _validDestinationFound)
        {
            _reachedDestination = true;
        }
    }
	
    private Vector3 CalculateWanderDestination()
    {
        // find a random node within the wander distance and set the agent's path to it
        var startNode = AstarPath.active.GetNearest(_ctx.transform.position, NNConstraint.Default).node;
        var nodes = PathUtilities.BFS(startNode, _ctx.MaxWanderNodeDistance);
        var singleRandomPoint = PathUtilities.GetPointsOnNodes(nodes, 1)[0];
		
        return singleRandomPoint;
    }
}
