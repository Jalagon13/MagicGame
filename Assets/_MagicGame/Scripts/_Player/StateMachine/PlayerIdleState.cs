using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : BaseState<PlayerStateMachine.PlayerState>
{
	private PlayerStateMachine _ctx;

	public PlayerIdleState(PlayerStateMachine.PlayerState key, StateMachine<PlayerStateMachine.PlayerState> context) : base(key, context)
	{
		_ctx = Context as PlayerStateMachine;
	}

	public override void EnterState()
	{
		Debug.Log($"Player entering idle");
		_ctx.IsMoving = false;
	}

	public override void ExitState()
	{
		
	}

	public override PlayerStateMachine.PlayerState GetNextState()
	{
		if(_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
			return PlayerStateMachine.PlayerState.Moving;
			
		return StateKey;
	}

	public override void FixedUpdate()
	{
		
	}
}
