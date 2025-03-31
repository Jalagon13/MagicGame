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
        Debug.Log("Idle State");
        _idleComplete = false;
        
        float idleDuration = UnityEngine.Random.Range(_ctx.MinIdleDuration, _ctx.MaxIdleDuration);
        
        if(idleDuration <= 0)
        {
            idleDuration = 0.0001f;
        }
        
        _idleTimer = new(idleDuration);
        _idleTimer.OnTimerEnd += IdleDone;
    }

    public override void ExitState()
    {
        Debug.Log($"Exiting idle state");
        if(_idleComplete)
        {
            Debug.Log($"Idle state complete, calculating new wander destionation");
            // Calculate new wander destination and desired direction for it
            Vector2? wanderDestination = GetRandomWanderDestinationBFS(_ctx.transform.position, _ctx.WanderRadius);
            
            if(wanderDestination.HasValue)
            {
                _ctx.WanderDestination = wanderDestination.Value;
                _ctx.DesiredDirection = _ctx.WanderDestination - (Vector2)_ctx.transform.position;
                Debug.Log($"Wander destination has value: {wanderDestination.Value}");
            }
            else
            {
                _ctx.WanderDestination = _ctx.transform.position;
                _ctx.DesiredDirection = _ctx.WanderDestination - (Vector2)_ctx.transform.position;
                Debug.Log($"Wander destination has no value, setting it to {_ctx.transform.position}");
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
        else if(!_idleComplete)
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
        Debug.Log("Starting GetRandomWanderDestinationBFS");

        Queue<Vector2> queue = new Queue<Vector2>();
        HashSet<Vector2> visited = new HashSet<Vector2>();

        var startTilePos = Vector2Int.FloorToInt(startPosition);
        Debug.Log($"Start tile pos: {startTilePos}");
        Vector2 centerStartTile = new Vector2(startTilePos.x + 0.5f, startTilePos.y + 0.5f);

        queue.Enqueue(centerStartTile);
        visited.Add(centerStartTile);

        List<Vector2> validTiles = new List<Vector2>();

        Debug.Log("Starting BFS loop");

        while (queue.Count > 0)
        {
            Vector2 current = queue.Dequeue();
            Debug.Log($"Dequeued {current}");

            // If the tile is walkable and within the radius, add it as a candidate
            if (Vector2.Distance(startPosition, current) <= wanderRadius && _ctx.IsPathUnObstructed(current))
            {
                validTiles.Add(current);
                Debug.Log($"Added {current} as a valid tile");
            }

            // Explore neighbors in all 4 directions (or 8 for diagonal movement)
            foreach (Vector2 neighbor in GetTileNeighbors(current))
            {
                Debug.Log($"Checking neighbor {neighbor}" + $"{_ctx.IsPathUnObstructed(neighbor)} {!visited.Contains(neighbor)} {Vector2.Distance(startPosition, neighbor) <= wanderRadius}");
                if (_ctx.IsPathUnObstructed(neighbor) && !visited.Contains(neighbor) && Vector2.Distance(startPosition, neighbor) <= wanderRadius)
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    Debug.Log($"Enqueued {neighbor}");
                }
            }
        }

        Debug.Log("Completed BFS loop");

        // Pick a random valid tile if any exist
        if(validTiles.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validTiles.Count);
            Debug.Log($"Returning {validTiles[randomIndex]}");
            return validTiles[randomIndex];
        }
        else
        {
            Debug.Log("No valid tiles found");
            return null;
        }
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
}