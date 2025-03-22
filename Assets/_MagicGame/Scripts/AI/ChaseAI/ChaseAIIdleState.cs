using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class ChaseAIIdleState : BaseState<ChaseAIStateMachine.ChaseAIState>
{
    private ChaseAIStateMachine _ctx;
    private Timer _idleTimer;
    private bool _idleComplete;

    public ChaseAIIdleState(ChaseAIStateMachine.ChaseAIState key, StateMachine<ChaseAIStateMachine.ChaseAIState> context) : base(key, context)
    {
        _ctx = Context as ChaseAIStateMachine;
    }

    public override void EnterState()
    {
        _idleComplete = false;
        Debug.Log("Idle State");
        _idleTimer = new(GetRandomeIdleDuration());
        _idleTimer.OnTimerEnd += IdleDone;
    }

    public override void ExitState()
    {
        if(_idleComplete)
        {
            // Calculate new wander destination and desired direction for it
            Vector2? wanderDestination = GetRandomWanderDestinationBFS(_ctx.transform.position, _ctx.WanderRadius);
            Debug.Log($"Idle Complete");
            if(wanderDestination.HasValue)
            {
                Debug.Log($"Current pos: {_ctx.transform.position} Wander Destination found: {wanderDestination.Value}");
                _ctx.WanderDestination = wanderDestination.Value;
                _ctx.DesiredDirection = _ctx.WanderDestination - (Vector2)_ctx.transform.position;
            }
            else
            {
                Debug.Log($"Wander NOT Destination return current pos: {_ctx.transform.position}");
                _ctx.WanderDestination = _ctx.transform.position;
                _ctx.DesiredDirection = _ctx.WanderDestination - (Vector2)_ctx.transform.position;
            }
        }
    }

    public override void FixedUpdate()
    {
        if(_ctx.CanMove)
        {
            _idleTimer.Tick(Time.fixedDeltaTime);
        }

        if (_ctx.Knockback.Velocity.magnitude > 0)
        {
            _ctx.Velocity = _ctx.Knockback.Velocity;
        }
        else
        {
            _ctx.Velocity = Vector2.zero;
        }

        _ctx.RigidBody2D.linearVelocity = _ctx.Velocity;
    }

    public override ChaseAIStateMachine.ChaseAIState GetNextState()
    {
        if((_idleComplete || _ctx.BreadCrumbPositionFound || _ctx.PlayerPositionFound) && _ctx.CanMove)
        {
            return ChaseAIStateMachine.ChaseAIState.Moving;
        }

        return StateKey;
    }

    private Vector2? GetRandomWanderDestinationBFS(Vector2 startPosition, float wanderRadius)
    {
        Queue<Vector2> queue = new Queue<Vector2>();
        HashSet<Vector2> visited = new HashSet<Vector2>();

        var startTilePos = Vector2Int.FloorToInt(startPosition);
        Vector2 centerStartTile = new Vector2(startTilePos.x + 0.5f, startTilePos.y + 0.5f);

        queue.Enqueue(centerStartTile);
        visited.Add(centerStartTile);

        List<Vector2> validTiles = new List<Vector2>();

        while (queue.Count > 0)
        {
            Vector2 current = queue.Dequeue();

            // If the tile is walkable and within the radius, add it as a candidate
            if (Vector2.Distance(startPosition, current) <= wanderRadius && _ctx.IsPathUnObstructed(current))
            {
                validTiles.Add(current);
            }

            // Explore neighbors in all 4 directions (or 8 for diagonal movement)
            foreach (Vector2 neighbor in GetTileNeighbors(current))
            {
                if (_ctx.IsPathUnObstructed(neighbor) && !visited.Contains(neighbor) && Vector2.Distance(startPosition, neighbor) <= wanderRadius)
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        // Pick a random valid tile if any exist
        return validTiles.Count > 0 ? validTiles[UnityEngine.Random.Range(0, validTiles.Count)] : null;
    }

    private List<Vector2> GetTileNeighbors(Vector2 tilePos)
    {
        return new List<Vector2>
        {
            tilePos + Vector2.up,    
            tilePos + Vector2.down,  
            tilePos + Vector2.left,  
            tilePos + Vector2.right  
        };
    }

    private void IdleDone(object sender, EventArgs e)
    {
        _idleTimer.OnTimerEnd -= IdleDone;
        _idleComplete = true;
    }

    private float GetRandomeIdleDuration()
    {
        return UnityEngine.Random.Range(_ctx.MinIdleDuration, _ctx.MaxIdleDuration);
    }
}