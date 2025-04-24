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

    [field: Tooltip("Smaller values = slower transition to desired direction")]
    [field: SerializeField] public float TurnSharpness { get; private set; } = 5f;
    [field: SerializeField] public float WanderSpeed { get; private set; } = 3f;
    [field: SerializeField] public float ChaseSpeed { get; private set; } = 4f;
    [field: SerializeField] public float KnockbackResist { get; private set; } = 0f;
    [field: SerializeField] public float MinIdleDuration { get; private set; } = 2.5f;
    [field: SerializeField] public float MaxIdleDuration { get; private set; } = 5f;
    [field: Tooltip("Detection radius for breadcrumbs and players")]
    [field: SerializeField] public float DetectionRadius { get; private set; } = 15f;
    [field: SerializeField] public float DetectionIntervalDuration { get; private set; } = 0.5f;
    [field: SerializeField] public float WanderRadius { get; private set; } = 10f;
    [field: Tooltip("How close the AI will get to a WanderDestination before stopping")]
    [field: SerializeField] public float StoppingDistance { get; private set; } = 0.25f;
    [field: SerializeField] public float StrafingDuration { get; private set; } = 0.25f;
    [field: SerializeField] public float StrafeIntensity { get; private set; } = 0.5f;
    [field: SerializeField] public bool WillChasePlayer { get; private set; } = true;
    [field: SerializeField] public bool OnlyChaseWhenProvoked { get; set; } = true;
    [field: Tooltip("If set to false, keep NPC on idle state")]
    [field: SerializeField] public bool CanMove { get; set; } = true;

    public Knockback Knockback { get; private set; }
    public Vector2 Velocity { get; set; }
    public Rigidbody2D RigidBody2D { get; private set; }
    public Vector2 DesiredDirection { get; set; }
    public bool PlayerPositionFound { get; private set; }
    public bool BreadCrumbPositionFound { get; private set; }
    public bool IsChasing { get; set; }
    public bool IsStrafing { get; private set; }
    public Vector2 WanderDestination { get; set; }
    public int StrafingDirection { get; private set; } = 1;
    public bool PlayerInSight { get; private set; }

    private Npc _npc;
    private NpcNetworkComponent _npcNetwork;
    private Vector2 _freshestBreadCrumbPosition = Vector2.zero;
    private Vector2 _closestPlayerPosition = Vector2.zero;
    private NetworkHealthState _healthState;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            _healthState = GetComponent<NetworkHealthState>();
            _npcNetwork = GetComponent<NpcNetworkComponent>();
            _npc = GetComponent<Npc>();
            _npc.OnServerNpcDamged += OnNpcDamged;

            _states[ChaseAIState.Idle] = new ChaseAIIdleState(ChaseAIState.Idle, this);
            _states[ChaseAIState.Moving] = new ChaseAIMoveState(ChaseAIState.Moving, this);
            _currentState = _states[ChaseAIState.Idle];

            Knockback = GetComponent<Knockback>();

            RigidBody2D = GetComponent<Rigidbody2D>();
            RigidBody2D.linearDamping = KnockbackResist;

            InvokeRepeating(nameof(TryToFindBreadcrumb), DetectionIntervalDuration, DetectionIntervalDuration);
        }
    }

    private void OnNpcDamged(object sender, Npc.OnNpcDamagedEventArgs e)
    {
        // Try to strafe behavior
        if(!IsStrafing && WillChasePlayer)
        {
            StartCoroutine(Strafing());
        }
    }

    private IEnumerator Strafing()
    {
        IsStrafing = true;
        StrafingDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;

        yield return new WaitForSeconds(StrafingDuration);
        IsStrafing = false;
    }

    private void TryToFindBreadcrumb()
    {
        if(!WillChasePlayer) return;

        if (OnlyChaseWhenProvoked && _healthState.HitPoints.Value >= _healthState.MaxHealth)
        {
            return;
        }

        PlayerPositionFound = false;
        BreadCrumbPositionFound = false;
        PlayerInSight = false;

        // Circle cast to find player or breadcrumb in detection radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, DetectionRadius, LayerMask.GetMask("Player", "Breadcrumb"));
        List<Collider2D> unObstructedColliders = new();

        foreach (Collider2D collider in colliders)
        {
            if(IsPathUnObstructed(collider.transform.position))
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
                if(player.CurrentPlayerBiome.Value == _npcNetwork.NpcBiomeType)
                {
                    float distance = Vector2.Distance(transform.position, player.transform.position);

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
                if(breadCrumb.Biome == _npcNetwork.NpcBiomeType)
                {
                    if(breadCrumb.RemainingLifeTime > highestLifetime)
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
            DesiredDirection = (_closestPlayerPosition - (Vector2)transform.position).normalized;
            IsChasing = true;
            PlayerInSight = true;
        }
        else if(BreadCrumbPositionFound)
        {
            // Debug.Log("Found closest breadcrumb! moving towards breadcrumb");
            DesiredDirection = (_freshestBreadCrumbPosition - (Vector2)transform.position).normalized;
            IsChasing = true;
        }
        else
        {
            IsChasing = false;
        }
    }

    public bool IsPathUnObstructed(Vector2 desiredEndpoint)
    {
        Vector2 direction = desiredEndpoint - (Vector2)transform.position;
        float distance = direction.magnitude;

        TilemapCollider2D localBiomePfWallCollider = Pathfinding.Instance.GetPathfindingWallCollider(_npcNetwork.NpcBiomeType);

        if(localBiomePfWallCollider == null) return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction.normalized, distance, LayerMask.GetMask("PathfindingWall"));

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
