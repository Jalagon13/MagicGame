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
		Debug.Log($"Idle state");
		_ctx.IsMoving = false;
	}

	public override void ExitState()
	{
		// Animation cleanup
	}

	public override PlayerStateMachine.PlayerState GetNextState()
	{
		if(_ctx.MoveVector != Vector2.zero && !_ctx.IsDead && _ctx.CanMove)
			return PlayerStateMachine.PlayerState.Moving;
			
		return StateKey;
	}

	public override void FixedUpdate()
	{
		_ctx.Velocity = Vector2.zero;
		_ctx.RigidBody2D.MovePosition(_ctx.RigidBody2D.position + _ctx.Velocity);
	}
}
