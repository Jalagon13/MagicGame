using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class BasicNpcMoveState : BaseState
{
    private BasicNpcStateMachine _ctx;
    private bool _destinationReached;
    private Vector2 _lastPosition;
    private float _timeNotMoved = 0f;
    private float _timeThreshold = 3.5f; // Every _timeThreshold seconds, check if pixie has moved _distanceThreshold
    private float _distanceThreshold = 0.2f;
    private bool _isStuck;
    
    private bool _hasDestination;
    private Vector2? _destination;

    public BasicNpcMoveState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log("Move State");
        _destinationReached = false; 
        _isStuck = false;
        _timeNotMoved = 0f;

        _destination = GetRandomWanderDestinationBFS();
        _hasDestination = _destination.HasValue;
        Debug.Log($"Has Destination: {_hasDestination}, Destination: {_destination}");
        if (_destination.HasValue)
        {
            Vector2 direction = _destination.Value - (Vector2)_ctx.ServerCharacter.transform.position;
            _ctx.ServerCharacter.Movement.StartMovement(direction.normalized);

            _isStuck = false;
            _timeNotMoved = 0f;
            _lastPosition = _ctx.ServerCharacter.transform.position;
        }
    }

    public override void ExitState()
    {
        _destinationReached = false;
        _isStuck = false;
        _hasDestination = false;
        _destination = null;
    }

    public override void UpdateState()
    {
        if (!_hasDestination) return;

        // Check if the destination has been reached
        float distanceToDestination = Vector2.Distance(_ctx.ServerCharacter.transform.position, _destination.Value);
        if (distanceToDestination <= _ctx.CharacterData.StoppingDistance)
        {
            _destinationReached = true;
        }

        // Check if the AI is stuck
        _timeNotMoved += Time.deltaTime;
        if (_timeNotMoved >= _timeThreshold)
        {
            float distanceMoved = Vector2.Distance(_lastPosition, _ctx.ServerCharacter.transform.position);
            if (distanceMoved < _distanceThreshold)
            {
                _isStuck = true;
            }
            _timeNotMoved = 0f;
            _lastPosition = _ctx.ServerCharacter.transform.position;
        }
    }

    public override void CheckSwitchStates()
    {
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            SwitchState(new AIStateData(AIState.Knockbacked));
        }
        else if (_ctx.IsChasing)
        {
            SwitchState(new AIStateData(AIState.Pursuing));
        }
        else if (!_hasDestination || _destinationReached || _isStuck)
        {
            Debug.Log($"Switching to Idle State, !_hasDestination: {!_hasDestination}, _destinationReached: {_destinationReached}, _isStuck: {_isStuck}");
            SwitchState(new AIStateData(AIState.Idle));
        }
    }

    private Vector2? GetRandomWanderDestinationBFS()
    {
        Queue<Vector2> queue = new Queue<Vector2>();
        HashSet<Vector2> visited = new HashSet<Vector2>();

        var startTilePos = Vector2Int.FloorToInt(_ctx.ServerCharacter.transform.position);
        Vector2 centerStartTile = new Vector2(startTilePos.x + 0.5f, startTilePos.y + 0.5f);

        queue.Enqueue(centerStartTile);
        visited.Add(centerStartTile);

        List<Vector2> validTiles = new List<Vector2>();

        while (queue.Count > 0)
        {
            Vector2 current = queue.Dequeue();

            // If the tile is walkable and within the radius, add it as a candidate
            if (Vector2.Distance(_ctx.ServerCharacter.transform.position, current) <= _ctx.ServerCharacter.Data.WanderRadius && _ctx.IsPathUnObstructed(current))
            {
                validTiles.Add(current);
            }

            // Explore neighbors in all 4 directions (or 8 for diagonal movement)
            foreach (Vector2 neighbor in GetTileNeighbors(current))
            {
                if (_ctx.IsPathUnObstructed(neighbor) && !visited.Contains(neighbor) && Vector2.Distance(_ctx.ServerCharacter.transform.position, neighbor) <= _ctx.ServerCharacter.Data.WanderRadius)
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        if (validTiles.Count > 0)
        {
            // Don’t pick a destination that’s effectively already “reached”
            float minDistance = _ctx.CharacterData.StoppingDistance + 0.1f; // small buffer
            List<Vector2> filtered = new List<Vector2>(validTiles);
            filtered.RemoveAll(tile => Vector2.Distance(_ctx.ServerCharacter.transform.position, tile) <= minDistance);

            Vector2 chosen;
            if (filtered.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, filtered.Count);
                chosen = filtered[randomIndex];
            }
            else
            {
                // fallback: all tiles were too close, just pick from original so we don’t return null
                int randomIndex = UnityEngine.Random.Range(0, validTiles.Count);
                chosen = validTiles[randomIndex];
            }

            return chosen;
        }

        return null;
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
}