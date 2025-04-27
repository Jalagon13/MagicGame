using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class VelocityBasedAnimator : NetworkBehaviour
{
    [field: SerializeField] public List<SpriteAnimationHandler> SpriteDirectionHandlers { get; private set; }

    private CardinalDirection _currentDirection;
    private bool _changedToIdleThisFrame;

    public void AnimateBasedOnVelocity(Vector2 velocity)
    {
        if (velocity.magnitude > 0.1f)
        {
            // Get the current velocity and convert it to a cardinal direction.
            _currentDirection = GetCardinalDirection(velocity);
            
            foreach (var handler in SpriteDirectionHandlers)
            {
                handler.PlayMoveAnimation(_currentDirection);
            }

            _changedToIdleThisFrame = false;
        }
        else if(!_changedToIdleThisFrame)
        {
            foreach (var handler in SpriteDirectionHandlers)
            {
                handler.PlayIdleAnimation(_currentDirection);
            }
            
            _changedToIdleThisFrame = true;
        }
    }

    private CardinalDirection GetCardinalDirection(Vector3 velocity)
    {
        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
        {
            return (velocity.x > 0) ? CardinalDirection.East : CardinalDirection.West;
        }
        else
        {
            return (velocity.y > 0) ? CardinalDirection.North : CardinalDirection.South;
        }
    }
}
