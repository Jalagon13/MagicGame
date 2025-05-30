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
		Debug.Log($"Player move state");
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
		if(_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
			return PlayerStateMachine.PlayerState.Idle;
	
		return StateKey;
	}
	
	public override void FixedUpdate()
	{
		_playWalkSoundTimer.Tick(Time.deltaTime);
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
