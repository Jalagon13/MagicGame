using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LivestockFleeingState : BaseState<LivestockStateMachine.LivestockState>
{
    private readonly LivestockStateMachine _ctx;
    private Vector3 _initialThreatenedPosition;
    private readonly int _safeDistance = 3;
    private bool _isSafe;
	
    public LivestockFleeingState(LivestockStateMachine.LivestockState key, StateMachine<LivestockStateMachine.LivestockState> context) : base(key, context)
    {
        _ctx = Context as LivestockStateMachine;
    }

    public override void EnterState()
    {
        // Debug.Log($"[Client {_ctx.NetworkManager.LocalClientId}] Entering fleeing");
		
        _initialThreatenedPosition = _ctx.transform.position;
        _ctx.IsMoving = true;
        _ctx.OnDirectionChange(_ctx.LookDirection);
        _isSafe = false;
    }

    public override void ExitState()
    {
        
    }

    public override LivestockStateMachine.LivestockState GetNextState()
    {
        return _isSafe ? LivestockStateMachine.LivestockState.Idle : StateKey;
    }

    public override void FixedUpdate()
    {
        // Calculate distance between current position and the initial threat position
        float initialThreatDistance = Vector2.Distance(_ctx.transform.position, _initialThreatenedPosition);
		
        // If agent is not in a safe distance from the initial position of the threat, keep fleeing
        if(initialThreatDistance < _safeDistance)
        {
            Vector3 fleeDirection = (_initialThreatenedPosition - _ctx.ThreatSource).normalized;
            Vector3 newPos = _ctx.transform.position + (fleeDirection * _safeDistance);
            _isSafe = false;
        }
        else
        {
            _isSafe = true;
        }
    }
}
