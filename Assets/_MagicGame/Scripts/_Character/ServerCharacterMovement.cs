using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MovementState
{
    Idle,
    Moving,
    Knockback,
    Pursuing
}

public class ServerCharacterMovement : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;

    [SerializeField] 
    private Rigidbody2D _rigidbody2D;
    public Rigidbody2D RigidBody2D => _rigidbody2D;
    
    private Knockback _knockback;
    private Vector2 _velocity;
    
    private Vector2 _desiredDirection;
    public Vector2 DesiredDirection => _desiredDirection;
    
    private float _speed;
    
    private void Awake()
    {
        _knockback = new(_serverCharacter);
        _knockback.OnKnockbackEnd += OnKnockbackEnd;
    }

    private void OnKnockbackEnd(object sender, EventArgs e)
    {
        _serverCharacter.MovementState.Value = MovementState.Idle;
    }

    public void FixedUpdateMovement()
    {
        if (_serverCharacter.MovementState.Value == MovementState.Idle)
            return;

        // Movement is handled here
        _knockback.UpdateKnockback(Time.fixedDeltaTime);

        if(_serverCharacter.MovementState.Value == MovementState.Knockback)
        {
            _velocity = _knockback.Velocity;
        }
        else
        {
            _velocity = Vector2.Lerp(_velocity, _desiredDirection * _speed, _serverCharacter.Data.TurnSharpness * Time.fixedDeltaTime);
        }
        
        _rigidbody2D.linearVelocity = _velocity;
    }
    
    public void StartIdle()
    {
        _serverCharacter.MovementState.Value = MovementState.Idle;
        _serverCharacter.AnimationHandler.PlayAnimation(_serverCharacter.AnimationHandler.AnimationConfig.SideIdleClip);
    }
    
    public void StartPursue(Vector2 desiredDirection)
    {
        _serverCharacter.MovementState.Value = MovementState.Pursuing;
        _desiredDirection = desiredDirection;
        _speed = _serverCharacter.Data.PursueSpeed;
        _serverCharacter.AnimationHandler.PlayAnimation(_serverCharacter.AnimationHandler.AnimationConfig.SideMoveClip);
    }
    
    public void StartMovement(Vector2 desiredDirection)
    {
        _serverCharacter.MovementState.Value = MovementState.Moving;
        _desiredDirection = desiredDirection;
        _speed = _serverCharacter.Data.BaseSpeed;
        _serverCharacter.AnimationHandler.PlayAnimation(_serverCharacter.AnimationHandler.AnimationConfig.SideMoveClip);
    }
    
    public void StartKnockback(Vector2 knockerPosition, float knockbackForce, bool inverse = false)
    {
        _serverCharacter.MovementState.Value = MovementState.Knockback;
        _knockback.ApplyKnockback(knockerPosition, knockbackForce, inverse);
    }
    
    public void SetDesiredDirection(Vector2 desiredDirection)
    {
        _desiredDirection = desiredDirection;
    }
}
