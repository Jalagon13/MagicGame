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

	protected override void EnterState(AIStateData stateData)
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
			SwitchState(new AIStateData(AIState.Moving));
		}
		else if(_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
		{
			SwitchState(new AIStateData(AIState.Knockbacked));
		}
	}

    public override void UpdateState()
	{
		
	}
}
