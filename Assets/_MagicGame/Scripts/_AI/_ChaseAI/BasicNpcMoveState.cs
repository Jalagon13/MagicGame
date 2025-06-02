using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

public class BasicNpcMoveState : BaseState<AIState>
{
    private BasicNpcStateMachine _ctx;
    private bool _destinationReached;
    private Vector2 _lastPosition;
    private float _timeNotMoved = 0f;
    private float _timeThreshold = 3.5f; // Every _timeThreshold seconds, check if pixie has moved _distanceThreshold
    private float _distanceThreshold = 0.2f;
    private bool _isStuck;
    private float _distanceToDestination;
    private Vector2 _startingPosition;
    
    private bool _hasDestination;
    private Vector2? _destination;

    public BasicNpcMoveState(AIState key, StateMachine<AIState> context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    public override void EnterState()
    {
        Debug.Log("Move State");
        _destination = GetRandomWanderDestinationBFS();

        _hasDestination = _destination.HasValue;
        if (_destination.HasValue)
        {
            Vector2 direction = _destination.Value - (Vector2)_ctx.ServerCharacter.transform.position;
            _ctx.ServerCharacter.Movement.StartMovement(direction);

            _isStuck = false;
            _timeNotMoved = 0f;
            _lastPosition = _ctx.ServerCharacter.transform.position;
            _distanceToDestination = Vector2.Distance(_ctx.ServerCharacter.transform.position, _destination.Value);
            _startingPosition = _ctx.ServerCharacter.transform.position;
        }
    }

    public override void ExitState()
    {

    }

    public override void UpdateState()
    {
        if(!_hasDestination) return;

        // Check if the destination has been reached
        float distanceToDestination = Vector2.Distance(_ctx.ServerCharacter.transform.position, _destination.Value);
        if (distanceToDestination <= _ctx.CharacterData.StoppingDistance || Vector2.Distance(_ctx.ServerCharacter.transform.position, _startingPosition) >= _distanceToDestination)
        {
            _destinationReached = true;
        }

        // Check if the AI is stuck
        _timeNotMoved += Time.fixedDeltaTime;
        if (_timeNotMoved >= _timeThreshold)
        {
            float distanceMoved = Vector2.Distance(_lastPosition, _ctx.ServerCharacter.transform.position);

            if (distanceMoved < _distanceThreshold)
            {
                // AI is stuck
                _isStuck = true;
            }

            // Reset timer and update last known position
            _timeNotMoved = 0f;
            _lastPosition = _ctx.ServerCharacter.transform.position;
        }
    }

    public override void CheckSwitchStates()
    {
        if (!_hasDestination || _destinationReached || _isStuck)
        {
            SwitchState(AIState.Idle);
        }

        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            SwitchState(AIState.Knockbacked);
        }

        if (_ctx.IsChasing)
        {
            SwitchState(AIState.Pursuing);
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

        // Pick a random valid tile if any exist
        if (validTiles.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, validTiles.Count);
            return validTiles[randomIndex];
        }
        else
        {
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
}