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
        Moving
    }

    public NetworkVariable<Vector2> Velocity { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public Vector2 DesiredDirection { get; set; }
    public bool PlayerPositionFound { get; private set; }
    public bool BreadCrumbPositionFound { get; private set; }
    public bool IsChasing { get; set; }
    public bool IsStrafing { get; private set; }
    public Vector2 WanderDestination { get; set; }
    public int StrafingDirection { get; private set; } = 1;
    public bool PlayerInSight { get; private set; }

    private Vector2 _freshestBreadCrumbPosition = Vector2.zero;
    private Vector2 _closestPlayerPosition = Vector2.zero;
    private VelocityBasedAnimator _velocityBasedAnimator;
    
    private ServerCharacter _serverCharacter;
    private ServerActionPlayer _serverActionPlayer;
    private Timer _breadCrumbDetectionTimer;
    private Timer _strafeTimer;

    public ChaseAIStateMachine(ServerCharacter serverCharacter, ServerActionPlayer serverActionPlayer)
    {
        _serverCharacter = serverCharacter;
        _serverActionPlayer = serverActionPlayer;
        
        _states[ChaseAIState.Idle] = new ChaseAIIdleState(ChaseAIState.Idle, _serverCharacter);
        _states[ChaseAIState.Moving] = new ChaseAIMoveState(ChaseAIState.Moving, _serverCharacter);
        _currentState = _states[ChaseAIState.Idle];
        
        if(serverCharacter.CharacterData.IsFriendly)
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
                // Try to strafe behavior
                if (!IsStrafing && _serverCharacter.CharacterData.WillChasePlayer)
                {
                    _strafeTimer = new Timer(_serverCharacter.CharacterData.StrafingDuration);
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

        _breadCrumbDetectionTimer?.Tick(Time.deltaTime);
        _strafeTimer?.Tick(Time.deltaTime);
    }

    private void EndStrafe(object sender, EventArgs e)
    {
        IsStrafing = false;
    }

    private void TryToFindBreadcrumb(object sender, EventArgs e)
    {
        if (_serverCharacter.CharacterData.OnlyChaseWhenProvoked && _serverCharacter.NetHealthState.HitPoints.Value >= _serverCharacter.CharacterData.BaseHP)
        {
            return;
        }

        PlayerPositionFound = false;
        BreadCrumbPositionFound = false;
        PlayerInSight = false;

        // Circle cast to find player or breadcrumb in detection radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_serverCharacter.transform.position, _serverCharacter.CharacterData.DetectionRadius, LayerMask.GetMask("Player", "Breadcrumb"));
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
                        _closestPlayerPosition = player.transform.position;
                        PlayerPositionFound = true;
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
                        _freshestBreadCrumbPosition = breadCrumb.transform.position;
                        BreadCrumbPositionFound = true;
                    }
                }
            }
        }

        if (PlayerPositionFound)
        {
            // Debug.Log("Found closest player! moving towards player");
            DesiredDirection = (_closestPlayerPosition - (Vector2)_serverCharacter.transform.position).normalized;
            IsChasing = true;
            PlayerInSight = true;
        }
        else if (BreadCrumbPositionFound)
        {
            // Debug.Log("Found closest breadcrumb! moving towards breadcrumb");
            DesiredDirection = (_freshestBreadCrumbPosition - (Vector2)_serverCharacter.transform.position).normalized;
            IsChasing = true;
        }
        else
        {
            IsChasing = false;
        }
    }

    protected void FixedUpdate()
    {
        if(_velocityBasedAnimator != null)
        {
            _velocityBasedAnimator.AnimateBasedOnVelocity(Velocity.Value);
        }
    }

    public bool IsPathUnObstructed(Vector2 desiredEndpoint)
    {
        Vector2 direction = desiredEndpoint - (Vector2)_serverCharacter.transform.position;
        float distance = direction.magnitude;

        TilemapCollider2D localBiomePfWallCollider = Pathfinding.Instance.GetPathfindingWallCollider(_serverCharacter.NpcVisibility.NpcBiomeType);

        if(localBiomePfWallCollider == null) return false;

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
}
