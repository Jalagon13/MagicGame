using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : BaseState<AIState>
{
	private PlayerStateMachine _ctx;

	public PlayerIdleState(AIState key, StateMachine<AIState> context) : base(key, context)
	{
		_ctx = Context as PlayerStateMachine;
	}

    public override void EnterState()
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
}
