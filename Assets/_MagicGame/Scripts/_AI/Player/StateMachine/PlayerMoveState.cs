using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class PlayerMoveState : BaseState<PlayerStateMachine.PlayerState>
{
	private PlayerStateMachine _ctx;
	private Timer _playWalkSoundTimer;
	private float _walkSoundCooldown = 0.28f;

	public PlayerMoveState(PlayerStateMachine.PlayerState key, StateMachine<PlayerStateMachine.PlayerState> context) : base(key, context)
	{
		_ctx = Context as PlayerStateMachine;
	}

	public override void EnterState()
	{
		PlayFootStepSound();
	
		_playWalkSoundTimer = new(_walkSoundCooldown);
		_playWalkSoundTimer.OnTimerEnd += PlayFootStepSound;
		
		_ctx.IsMoving = true;
	}

	public override void ExitState()
	{
		_playWalkSoundTimer.OnTimerEnd -= PlayFootStepSound;
		_playWalkSoundTimer.IsPaused = true;
		_playWalkSoundTimer = null;
	}

	public override PlayerStateMachine.PlayerState GetNextState()
	{
		if(_ctx.MoveVector.magnitude == 0 && _ctx.Velocity.magnitude < 0.25f || _ctx.IsDead || !_ctx.CanMove)
			return PlayerStateMachine.PlayerState.Idle;
	
		return StateKey;
	}
	
	public override void FixedUpdate()
	{
		_playWalkSoundTimer.Tick(Time.deltaTime);

		Vector2 desiredDirection = _ctx.MoveVector.normalized; 

		if(_ctx.Knockback.KnockbackActive)
		{
			_ctx.Velocity = desiredDirection + _ctx.Knockback.Velocity;
		}
		else
		{
			_ctx.Velocity = Vector2.Lerp(_ctx.Velocity, desiredDirection * _ctx.PlayerStats.CurrentSpeed, _ctx.PlayerStats.TurnSharpness * Time.fixedDeltaTime);
		}
		
		_ctx.RigidBody2D.linearVelocity = _ctx.Velocity;
	}
	
	private void PlayFootStepSound(object sender, EventArgs e)
	{
		PlayFootStepSound();
	
		_playWalkSoundTimer.Reset();
	}
	
	private void PlayFootStepSound()
	{
		if(!_ctx.IsDead)
		{
			SoundManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerFootsteps, Player.LocalClientInstance.transform.position);
		}
	}
}
