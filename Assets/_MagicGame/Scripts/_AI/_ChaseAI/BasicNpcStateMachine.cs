using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System;
using System.Collections;

// Wanders until it finds a player or breadcrumb to move to
public class BasicNpcStateMachine : StateMachine
{
    public bool IsChasing { get; private set; }
    public bool IsStrafing { get; private set; }
    public bool PlayerInSight { get; private set; }
    public bool IsAngry { get; private set; }
    public Vector2? PursueDestination { get; private set; } = Vector2.zero;
    
    private Timer _breadCrumbDetectionTimer;
    private Timer _strafeTimer;

    private bool _playerPositionFound, _breadCrumbPositionFound;
    public int StrafingDirection { get; private set; }

    public BasicNpcStateMachine(ServerCharacter serverCharacter)
    {
        _serverCharacter = serverCharacter;
        
        _states[AIState.Idle] = new BasicNpcIdleState(AIState.Idle, this);
        _states[AIState.Moving] = new BasicNpcMoveState(AIState.Moving, this);
        _states[AIState.Knockbacked] = new BasicNpcKnockbackState(AIState.Knockbacked, this);
        _states[AIState.Pursuing] = new BasicNpcPursueState(AIState.Pursuing, this);
        _currentState = _states[AIState.Idle];
    }

    public override void OwnerInitialization()
    {
        if (!CharacterData.IsFriendly)
        {
            _breadCrumbDetectionTimer = new Timer(0.5f);
            _breadCrumbDetectionTimer.OnTimerEnd -= TryToFindBreadcrumb;
            _breadCrumbDetectionTimer.OnTimerEnd += TryToFindBreadcrumb;
        }
    }

    public override void ReceiveHP(ServerCharacter inflicter, int amount)
    {
        if (inflicter != null)
        {
            if (amount < 0)
            {
                // Damaged
                IsAngry = true;

                // Try to strafe behavior
                if (!IsStrafing && CharacterData.WillChasePlayer)
                {
                    _strafeTimer = new Timer(CharacterData.StrafingDuration);
                    _strafeTimer.OnTimerEnd -= EndStrafe;
                    _strafeTimer.OnTimerEnd += EndStrafe;
                    StrafingDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
                    IsStrafing = true;
                }
            }
            else
            {
                // Healed
            }
        }
    }

    public override void UpdateAI()
    {
        base.UpdateAI();

        if (!CharacterData.IsFriendly)
        {
            _breadCrumbDetectionTimer?.Tick(Time.deltaTime);
            _strafeTimer?.Tick(Time.deltaTime);
        }
    }

    private void EndStrafe(object sender, EventArgs e)
    {
        IsStrafing = false;
    }

    private void TryToFindBreadcrumb(object sender, EventArgs e)
    {
        if (CharacterData.OnlyChaseWhenProvoked && _serverCharacter.NetHealthState.HitPoints.Value >= CharacterData.BaseHealth)
        {
            return;
        }

        _playerPositionFound = false;
        _breadCrumbPositionFound = false;
        PlayerInSight = false;

        // Circle cast to find player or breadcrumb in detection radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_serverCharacter.transform.position, CharacterData.DetectionRadius, LayerMask.GetMask("Player", "Breadcrumb"));
        List<Collider2D> unObstructedColliders = new();

        foreach (Collider2D collider in colliders)
        {
            if (IsPathUnObstructed(collider.transform.position))
            {
                unObstructedColliders.Add(collider);
            }
        }

        float closestDistance = float.MaxValue;
        float highestLifetime = 0f;

        foreach (Collider2D unObstructedCollider in unObstructedColliders)
        {
            if (unObstructedCollider.transform.root.TryGetComponent(out Player player))
            {
                if (player.CurrentBiome.Value == _serverCharacter.CurrentBiome)
                {
                    float distance = Vector2.Distance(_serverCharacter.transform.position, player.transform.position);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        PursueDestination = player.transform.position;
                        _playerPositionFound = true;
                    }
                }
            }
            else if (unObstructedCollider.TryGetComponent(out BreadCrumb breadCrumb))
            {
                if (breadCrumb.Biome == _serverCharacter.CurrentBiome)
                {
                    if (breadCrumb.RemainingLifeTime > highestLifetime)
                    {
                        highestLifetime = breadCrumb.RemainingLifeTime;
                        PursueDestination = breadCrumb.transform.position;
                        _breadCrumbPositionFound = true;
                    }
                }
            }
        }

        if (_playerPositionFound || _breadCrumbPositionFound)
        {
            // Debug.Log("Found closest player! moving towards player");
            
            IsChasing = true;
            
            if(_playerPositionFound)
                PlayerInSight = true;
        }
        else
        {
            IsChasing = false;
        }

        _breadCrumbDetectionTimer.Reset();
    }

    

    public bool IsPathUnObstructed(Vector2 desiredEndpoint)
    {
        Vector2 direction = desiredEndpoint - (Vector2)_serverCharacter.transform.position;
        float distance = direction.magnitude;

        TilemapCollider2D localBiomePfWallCollider = Pathfinding.Instance.GetPathfindingWallCollider(_serverCharacter.CurrentBiome);

        if (localBiomePfWallCollider == null) return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(_serverCharacter.transform.position, direction.normalized, distance, LayerMask.GetMask("PathfindingWall"));

        foreach (var hit in hits)
        {
            if (hit.collider == localBiomePfWallCollider)
            {
                return false;
            }
        }

        return true;
    }

    // protected void FixedUpdate()
    // {
    //     if(_velocityBasedAnimator != null)
    //     {
    //         _velocityBasedAnimator.AnimateBasedOnVelocity(Velocity.Value);
    //     }
    // }
}
