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

    private Transform _pursueTarget;
    private Vector2 _pursueDirectionOffset;
    private float _strafeSpeedMultiplier;
    private bool _isDead;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _knockback = new(_serverCharacter);
            _serverCharacter.NetLifeState.LifeState.OnValueChanged += OnLifeStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            _serverCharacter.NetLifeState.LifeState.OnValueChanged -= OnLifeStateChanged;
        }
    }

    private void OnLifeStateChanged(LifeState previousValue, LifeState newValue)
    {
        if (previousValue == LifeState.Alive && newValue == LifeState.Dead)
        {
            _isDead = true;
        }
        else if (previousValue == LifeState.Dead && newValue == LifeState.IFrame)
        {
            _isDead = false;
            StartIdle();
        }
    }

    public void FixedUpdateMovement()
    {
        if(_isDead) return; // Prevent movement if the character is dead.
    
        if (_serverCharacter.MovementState.Value == MovementState.Idle)
        {
            _desiredDirection = Vector2.zero;
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
            if (_serverCharacter.MovementState.Value == MovementState.Pursuing && _pursueTarget != null)
            {
                Vector2 baseDirection = ((Vector2)_pursueTarget.position - (Vector2)_serverCharacter.transform.position).normalized;
                _desiredDirection = (baseDirection + _pursueDirectionOffset).normalized;
            }
            
            float currentSpeed = _serverCharacter.Stats.MovementSpeed.GetValue();

            if (_serverCharacter.MovementState.Value == MovementState.Fleeing)
            {
                currentSpeed *= _serverCharacter.Data.FleeSpeedMultiplier;
            }
            else if (_serverCharacter.MovementState.Value == MovementState.Pursuing)
            {
                currentSpeed *= _serverCharacter.Data.PursueSpeedMultiplier;
                currentSpeed *= _strafeSpeedMultiplier;
            }
            
            _velocity = Vector2.Lerp(_velocity, _desiredDirection * currentSpeed, _serverCharacter.Data.TurnSharpness * Time.fixedDeltaTime);
        }

        if (_desiredDirection != Vector2.zero)
        {
            _serverCharacter.CardinalDirection.Value = CardinalDirectionFromDesiredDirection(_desiredDirection);
        }
        
        _rigidbody2D.linearVelocity = _velocity;
    }
    
    public void StartIdle()
    {
        _velocity = Vector2.zero;
        _desiredDirection = Vector2.zero;
        _pursueTarget = null;
        _serverCharacter.MovementState.Value = MovementState.Idle;
    }
    
    public void StartPursue(Transform target)
    {
        _pursueTarget = target;
        _serverCharacter.MovementState.Value = MovementState.Pursuing;
    }
    
    public void StartMovement(Vector2 desiredDirection)
    {
        _desiredDirection = desiredDirection.normalized;
        _pursueTarget = null;
        _serverCharacter.MovementState.Value = MovementState.Moving;
    }
    
    public void StartFlee(Vector2 fleeDirection)
    {
        _desiredDirection = fleeDirection.normalized;
        _pursueTarget = null;
        _serverCharacter.MovementState.Value = MovementState.Fleeing;
    }

    public void StartKnockback(Vector2 knockerPosition, float knockbackForce, bool inverse = false)
    {
        Vector2 knockbackDirection = ((Vector2)_serverCharacter.transform.position - knockerPosition).normalized;
        _serverCharacter.MovementState.Value = MovementState.Knockback;
        _knockback.ApplyKnockback(knockerPosition, knockbackForce, inverse);
    }

    private CardinalDirection CardinalDirectionFromDesiredDirection(Vector2 desiredDirection)
    {
        if (Math.Abs(desiredDirection.x) > Math.Abs(desiredDirection.y))
        {
            return desiredDirection.x > 0 ? CardinalDirection.East : CardinalDirection.West;
        }
        else
        {
            return desiredDirection.y > 0 ? CardinalDirection.North : CardinalDirection.South;
        }
    }

    public void SetDesiredDirection(Vector2 desiredDirection)
    {
        _desiredDirection = desiredDirection.normalized;
    }

    public void SetPursueOffset(Vector2 offset, float speedMultiplier)
    {
        _pursueDirectionOffset = offset;
        _strafeSpeedMultiplier = speedMultiplier;
    }
}
