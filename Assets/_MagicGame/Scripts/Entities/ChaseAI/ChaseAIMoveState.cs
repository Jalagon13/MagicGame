using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class ChaseAIMoveState : BaseState<ChaseAIStateMachine.ChaseAIState>
{
    private ChaseAIStateMachine _ctx;
    private bool _destinationReached;
    private Vector2 _lastPosition;
    private float _timeNotMoved = 0f;
    private float _timeThreshold = 3.5f; // Every _timeThreshold seconds, check if pixie has moved _distanceThreshold
    private float _distanceThreshold = 0.2f;
    private bool _isStuck;

    public ChaseAIMoveState(ChaseAIStateMachine.ChaseAIState key, StateMachine<ChaseAIStateMachine.ChaseAIState> context) : base(key, context)
    {
        _ctx = Context as ChaseAIStateMachine;
    }

    public override void EnterState()
    {
        Debug.Log("Move State");

        _isStuck = false;
        _timeNotMoved = 0f;
        _lastPosition = _ctx.transform.position;
        _ctx.IsChasing = _ctx.BreadCrumbPositionFound || _ctx.PlayerPositionFound;

        if(!_ctx.IsChasing)
        {
            _destinationReached = false;
        }
    }

    public override void ExitState()
    {

    }

    public override void FixedUpdate()
    {
        if (!_ctx.IsChasing)
        {
            // Check if the destination has been reached
            float distanceToDestination = Vector2.Distance(_ctx.transform.position, _ctx.WanderDestination);
            if (distanceToDestination <= _ctx.StoppingDistance)
            {
                Debug.Log($"Destination reached");
                _destinationReached = true;
            }
        }

        Vector2 desiredDirection = _ctx.DesiredDirection.normalized;

        // Strafe while chasing
        if (_ctx.IsChasing && _ctx.IsStrafing)
        {
            // Get a perpendicular vector (left or right)
            Vector2 perpendicular = new Vector2(-desiredDirection.y, desiredDirection.x) * _ctx.StrafingDirection;

            // Apply strafing effect by blending it into the desired direction
            desiredDirection += perpendicular * _ctx.StrafeIntensity;
            desiredDirection = desiredDirection.normalized; // Normalize to maintain consistent speed
        }

        if (_ctx.Knockback.Velocity.magnitude > 0)
        {
            _ctx.Velocity = desiredDirection + _ctx.Knockback.Velocity;
        }
        else
        {
            _ctx.Velocity = Vector2.Lerp(_ctx.Velocity, desiredDirection * _ctx.Speed, _ctx.TurnSharpness * Time.fixedDeltaTime);
        }

        _ctx.RigidBody2D.linearVelocity = _ctx.Velocity;

        _timeNotMoved += Time.fixedDeltaTime;
        if (_timeNotMoved >= _timeThreshold)
        {
            float distanceMoved = Vector2.Distance(_lastPosition, _ctx.transform.position);

            if (distanceMoved < _distanceThreshold)
            {
                // AI is stuck
                _isStuck = true;
            }

            // Reset timer and update last known position
            _timeNotMoved = 0f;
            _lastPosition = _ctx.transform.position;
        }
    }

    public override ChaseAIStateMachine.ChaseAIState GetNextState()
    {
        if (_isStuck)
        {
            Debug.Log("Pixie is stuck, going back to idle");
            return ChaseAIStateMachine.ChaseAIState.Idle;
        }

        if(_ctx.IsChasing)
        {
            if (!_ctx.BreadCrumbPositionFound && !_ctx.PlayerPositionFound)
            {
                return ChaseAIStateMachine.ChaseAIState.Idle;
            }
        }
        else
        {
            if(_destinationReached)
            {
                Debug.Log($"Destination reached. going back to idle");
                return ChaseAIStateMachine.ChaseAIState.Idle;
            }
        }

        return StateKey;
    }
}