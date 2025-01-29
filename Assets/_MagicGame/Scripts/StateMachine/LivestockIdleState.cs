using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LivestockIdleState : BaseState<LivestockStateMachine.LivestockState>
{
    private readonly LivestockStateMachine _ctx;
    private bool _idleDone;

    public LivestockIdleState(LivestockStateMachine.LivestockState key, StateMachine<LivestockStateMachine.LivestockState> context) : base(key, context)
    {
        _ctx = Context as LivestockStateMachine;
    }

    public override void EnterState()
    {
        // Debug.Log($"[Client {_ctx.NetworkManager.LocalClientId}] Entering Idle");
		
        _idleDone = false;
        _ctx.IsMoving = false;
        _ctx.OnDirectionChange(_ctx.LookDirection);
		
        _ctx.StartCoroutine(PlayIdleDuration());
    }

    public override void ExitState()
    {
	
    }
	
    public override LivestockStateMachine.LivestockState GetNextState()
    {
        return _idleDone ? LivestockStateMachine.LivestockState.Wandering : StateKey;
    }
	
    public override void FixedUpdate()
    {
		
    }
	
    private IEnumerator PlayIdleDuration()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(_ctx.MinIdleTime, _ctx.MaxIdleTime));
        _idleDone = true;
    }
}
