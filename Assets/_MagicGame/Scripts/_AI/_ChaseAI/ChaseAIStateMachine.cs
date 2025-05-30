using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System;
using System.Collections;

// Wanders until it finds a player or breadcrumb to move to
public class ChaseAIStateMachine : StateMachine<ChaseAIStateMachine.ChaseAIState>
{
    public enum ChaseAIState
    {
        Idle,
        Moving,
        Knockback,
        Pursuing
    }

    public bool IsChasing { get; private set; }
    public bool IsStrafing { get; private set; }
    public bool PlayerInSight { get; private set; }
    public bool IsAngry { get; private set; }
    public Vector2? PursueDestination { get; private set; } = Vector2.zero;
    // private VelocityBasedAnimator _velocityBasedAnimator;
    
    private ServerCharacter _serverCharacter;
    public ServerCharacter ServerCharacter => _serverCharacter;
    public CharacterDataSO CharacterData => _serverCharacter.Data;
    
    private ServerActionPlayer _serverActionPlayer;
    public ServerActionPlayer ServerActionPlayer => _serverActionPlayer;
    
    private Timer _breadCrumbDetectionTimer;
    private Timer _strafeTimer;

    private bool _playerPositionFound, _breadCrumbPositionFound;
    public int StrafingDirection { get; private set; }

    public ChaseAIStateMachine(ServerCharacter serverCharacter, ServerActionPlayer serverActionPlayer)
    {
        _serverCharacter = serverCharacter;
        _serverActionPlayer = serverActionPlayer;
        
        _states[ChaseAIState.Idle] = new ChaseAIIdleState(ChaseAIState.Idle, this);
        _states[ChaseAIState.Moving] = new ChaseAIMoveState(ChaseAIState.Moving, this);
        _states[ChaseAIState.Knockback] = new ChaseAIKnockbackState(ChaseAIState.Knockback, this);
        _states[ChaseAIState.Pursuing] = new ChaseAIPursueState(ChaseAIState.Pursuing, this);
        _currentState = _states[ChaseAIState.Idle];
        
        if(!CharacterData.IsFriendly)
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
        if (CharacterData.OnlyChaseWhenProvoked && _serverCharacter.NetHealthState.HitPoints.Value >= CharacterData.BaseHP)
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
                if (player.CurrentPlayerBiome.Value == _serverCharacter.NpcVisibility.NpcBiomeType)
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
                if (breadCrumb.Biome == _serverCharacter.NpcVisibility.NpcBiomeType)
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

        TilemapCollider2D localBiomePfWallCollider = Pathfinding.Instance.GetPathfindingWallCollider(_serverCharacter.NpcVisibility.NpcBiomeType);

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
