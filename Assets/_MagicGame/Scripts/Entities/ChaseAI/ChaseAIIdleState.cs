using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class ChaseAIIdleState : BaseState<ChaseAIStateMachine.ChaseAIState>
{
    private ChaseAIStateMachine _ctx;
    private Timer _idleTimer;
    private bool _idleComplete;

    public ChaseAIIdleState(ChaseAIStateMachine.ChaseAIState key, StateMachine<ChaseAIStateMachine.ChaseAIState> context) : base(key, context)
    {
        _ctx = Context as ChaseAIStateMachine;
    }

    public override void EnterState()
    {
        Debug.Log("Idle State");
        _idleTimer = new(GetRandomeIdleDuration());
        _idleTimer.OnTimerEnd += IdleDone;
    }

    public override void ExitState()
    {
        
    }

    public override void FixedUpdate()
    {
        _idleTimer.Tick(Time.fixedDeltaTime);

        if (_ctx.Knockback.Velocity.magnitude > 0)
        {
            _ctx.Velocity = _ctx.Knockback.Velocity;
        }
        else
        {
            _ctx.Velocity = Vector2.zero;
        }

        _ctx.RigidBody2D.linearVelocity = _ctx.Velocity;
    }

    public override ChaseAIStateMachine.ChaseAIState GetNextState()
    {
        if(/* _idleComplete || */ _ctx.BreadCrumbPositionFound || _ctx.PlayerPositionFound)
        {
            return ChaseAIStateMachine.ChaseAIState.Moving;
        }

        return StateKey;
    }

    private void IdleDone(object sender, EventArgs e)
    {
        _idleTimer.OnTimerEnd -= IdleDone;
        _idleComplete = true;
    }

    private float GetRandomeIdleDuration()
    {
        return UnityEngine.Random.Range(_ctx.MinIdleDuration, _ctx.MaxIdleDuration);
    }
}