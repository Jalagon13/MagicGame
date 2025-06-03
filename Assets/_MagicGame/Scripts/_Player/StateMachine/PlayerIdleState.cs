using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : BaseState
{
	private PlayerStateMachine _ctx;

	public PlayerIdleState(AIState key, StateMachine context) : base(key, context)
	{
		_ctx = Context as PlayerStateMachine;
	}

	protected override void EnterState()
	{
		Debug.Log($"Player entering idle");
		// _ctx.IsMoving = false;
	}

	public override void ExitState()
	{
		
	}

	public override void CheckSwitchStates()
	{
		if (_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
		{
			SwitchState(AIState.Moving);
		}
	}

    public override void UpdateState()
	{
		
	}

    public override void ClientEnterState()
    {
        Debug.Log($"[Client {_ctx.ServerCharacter.OwnerClientId}] Player entering idle");
    }
    
	public override void ClientExitState()
	{
		Debug.Log($"[Client {_ctx.ServerCharacter.OwnerClientId}] Player exiting idle");
	}
	
	public override void ClientUpdateState()
	{
		
	}
}
