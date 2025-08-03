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
    Pursuing,
    Fleeing
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
    public Vector2 Velocity => _velocity;
    
    private Vector2 _desiredDirection;
    public Vector2 DesiredDirection => _desiredDirection;
    
    private void Awake()
    {
        _knockback = new(_serverCharacter);
    }

    public void FixedUpdateMovement()
    {
        if (_serverCharacter.MovementState.Value == MovementState.Idle)
        {
            _velocity = Vector2.zero;
            return;
        }

        if(_serverCharacter.Data.CanBeKnockedBack && _serverCharacter.MovementState.Value == MovementState.Knockback)
        {
            _knockback.UpdateKnockback(Time.fixedDeltaTime);
            _velocity = _knockback.Velocity;
            
            if(!_knockback.KnockbackActive)
            {
                _serverCharacter.MovementState.Value = _desiredDirection == Vector2.zero ? MovementState.Idle : MovementState.Moving;
                return;
            }
        }
        else if(_serverCharacter.Data.CanMove)
        {
            float currentSpeed = _serverCharacter.Stats.MovementSpeed.GetValue() * (_serverCharacter.MovementState.Value == MovementState.Fleeing ? _serverCharacter.Data.FleeSpeedMultiplier : 1f);
            _velocity = Vector2.Lerp(_velocity, _desiredDirection * currentSpeed, _serverCharacter.Data.TurnSharpness * Time.fixedDeltaTime);
        }
        
        _rigidbody2D.linearVelocity = _velocity;
    }
    
    public void StartIdle()
    {
        _desiredDirection = Vector2.zero;
        _serverCharacter.MovementState.Value = MovementState.Idle;
    }
    
    public void StartPursue(Vector2 desiredDirection)
    {
        _desiredDirection = desiredDirection.normalized;
        // _speed = _serverCharacter.Data.PursueSpeed; // NTFS: Maybe just make this a buff or something
        _serverCharacter.CardinalDirection.Value = CardinalDirectionFromDesiredDirection();
        _serverCharacter.MovementState.Value = MovementState.Pursuing;
    }
    
    public void StartMovement(Vector2 desiredDirection)
    {
        _desiredDirection = desiredDirection.normalized;
        _serverCharacter.CardinalDirection.Value = CardinalDirectionFromDesiredDirection();
        _serverCharacter.MovementState.Value = MovementState.Moving;
    }
    
    public void StartFlee(Vector2 fleeDirection)
    {
        _desiredDirection = fleeDirection.normalized;
        _serverCharacter.CardinalDirection.Value = CardinalDirectionFromDesiredDirection();
        _serverCharacter.MovementState.Value = MovementState.Fleeing;
    }

    public void StartKnockback(Vector2 knockerPosition, float knockbackForce, bool inverse = false)
    {
        _serverCharacter.MovementState.Value = MovementState.Knockback;
        _knockback.ApplyKnockback(knockerPosition, knockbackForce, inverse);
    }

    private CardinalDirection CardinalDirectionFromDesiredDirection()
    {
        if (Math.Abs(_desiredDirection.x) > Math.Abs(_desiredDirection.y))
        {
            return _desiredDirection.x > 0 ? CardinalDirection.East : CardinalDirection.West;
        }
        else
        {
            return _desiredDirection.y > 0 ? CardinalDirection.North : CardinalDirection.South;
        }
    }

    public void SetDesiredDirection(Vector2 desiredDirection)
    {
        _desiredDirection = desiredDirection.normalized;
    }
}
