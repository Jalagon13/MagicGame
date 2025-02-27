using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

// Wanders until it finds a player or breadcrumb to move to
public class ChaseAIStateMachine : StateMachine<ChaseAIStateMachine.ChaseAIState>
{
    public enum ChaseAIState
    {
        Idle,
        Moving
    }

    [Tooltip("Smaller values = slower transition to desired direction")]
    [field: SerializeField] public float TurnSharpness { get; private set; } = 5f;
    [field: SerializeField] public float Speed { get; private set; } = 3f;
    [field: SerializeField] public float KnockbackResist { get; private set; } = 0f;
    [field: SerializeField] public float MinIdleDuration { get; private set; } = 2.5f;
    [field: SerializeField] public float MaxIdleDuration { get; private set; } = 5f;
    [Tooltip("Detection radius for breadcrumbs and players")]
    [field: SerializeField] public float DetectionRadius { get; private set; } = 15f;
    [field: SerializeField] public float DetectionIntervalDuration { get; private set; } = 0.5f;
    [Tooltip("Thickness of the raycast used to check for obstructions when navigating to a player or breadcrumb. Adjust approximately to the radius of the AI's collider")]
    [field: SerializeField] public float ObstrcutionCheckLineThickness { get; private set; } = 0.25f;
    [field: SerializeField] public float WanderRadius { get; private set; } = 10f;
    [Tooltip("How close the AI will get to a WanderDestination before stopping")]
    [field: SerializeField] public float StoppingDistance { get; private set; } = 0.25f;

    public Knockback Knockback { get; private set; }
    public Vector2 Velocity { get; set; }
    public Rigidbody2D RigidBody2D { get; private set; }
    public Vector2 DesiredDirection { get; set; }
    public bool PlayerPositionFound { get; private set; }
    public bool BreadCrumbPositionFound { get; private set; }
    public bool IsChasing { get; set; }
    public Vector2 WanderDestination { get; set; }

    private NpcNetworkComponent _npc;
    private Vector2 _freshestBreadCrumbPosition = Vector2.zero;
    private Vector2 _closestPlayerPosition = Vector2.zero;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            _npc = GetComponent<NpcNetworkComponent>();

            _states[ChaseAIState.Idle] = new ChaseAIIdleState(ChaseAIState.Idle, this);
            _states[ChaseAIState.Moving] = new ChaseAIMoveState(ChaseAIState.Moving, this);
            _currentState = _states[ChaseAIState.Idle];

            Knockback = GetComponent<Knockback>();

            RigidBody2D = GetComponent<Rigidbody2D>();
            RigidBody2D.linearDamping = KnockbackResist;

            InvokeRepeating(nameof(TryToFindBreadcrumb), DetectionIntervalDuration, DetectionIntervalDuration);
        }
    }
    

    protected override void FixedUpdate()
    {
        if (!IsServer) return;

        base.FixedUpdate();


    }

    private void TryToFindBreadcrumb()
    {
        PlayerPositionFound = false;
        BreadCrumbPositionFound = false;

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
                if(player.CurrentPlayerBiome.Value == _npc.NpcBiomeType)
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
                if(breadCrumb.Biome == _npc.NpcBiomeType)
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

        TilemapCollider2D localBiomePfWallCollider = Pathfinding.Instance.GetPathfindingWallCollider(_npc.NpcBiomeType);

        if(localBiomePfWallCollider == null) return false;

        RaycastHit2D[] hits = Physics2D.CircleCastAll(transform.position, ObstrcutionCheckLineThickness, direction.normalized, distance, LayerMask.GetMask("PathfindingWall"));

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
