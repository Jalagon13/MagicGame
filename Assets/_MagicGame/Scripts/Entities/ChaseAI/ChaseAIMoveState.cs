using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class ChaseAIMoveState : BaseState<ChaseAIStateMachine.ChaseAIState>
{
    private ChaseAIStateMachine _ctx;

    public ChaseAIMoveState(ChaseAIStateMachine.ChaseAIState key, StateMachine<ChaseAIStateMachine.ChaseAIState> context) : base(key, context)
    {
        _ctx = Context as ChaseAIStateMachine;
    }

    public override void EnterState()
    {
        Debug.Log("Move State");
    }

    public override void ExitState()
    {

    }

    public override void FixedUpdate()
    {
        Vector2 desiredDirection = _ctx.DesiredDirection.normalized;

        if (_ctx.Knockback.Velocity.magnitude > 0)
        {
            _ctx.Velocity = desiredDirection + _ctx.Knockback.Velocity;
        }
        else
        {
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, desiredDirection * _ctx.Speed, _ctx.TurnSharpness * Time.fixedDeltaTime);
        }

        _ctx.RigidBody2D.linearVelocity = _ctx.Velocity;
    }

    public override ChaseAIStateMachine.ChaseAIState GetNextState()
    {
        if (!_ctx.BreadCrumbPositionFound && !_ctx.PlayerPositionFound)
        {
            return ChaseAIStateMachine.ChaseAIState.Idle;
        }

        return StateKey;
    }
}