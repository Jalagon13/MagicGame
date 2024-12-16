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
		_ctx.IsMoving = false;
	}

	public override void ExitState()
	{
		// Animation cleanup
	}

	public override PlayerStateMachine.PlayerState GetNextState()
	{
		if(_ctx.MoveVector.magnitude > 0 && !_ctx.IsDead)
			return PlayerStateMachine.PlayerState.Moving;
			
		return StateKey;
	}

	public override void FixedUpdate()
	{
		if(!_ctx.Knockback.IsBeingKnockedBack)
		{
			_ctx.RigidBody2D.MovePosition(_ctx.transform.position);
		}
	}
}
