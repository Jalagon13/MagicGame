using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ServerSpriteAnimHandler : NetworkBehaviour
{
    [SerializeField]
    private AnimationConfigSO _animConfig;
    private NetworkAnimator _networkAnimator;

    private void Awake()
    {
        _networkAnimator = GetComponent<NetworkAnimator>();
    }

    public void PlayAnimation(MovementState movementState, CardinalDirection cardinalDirection)
    {
        UpdateSpriteOrientationClientRpc(cardinalDirection);

        AnimationClip clip = null;

        if (movementState == MovementState.Idle)
        {
            clip = cardinalDirection switch
            {
                CardinalDirection.North => _animConfig.BackIdleClip,
                CardinalDirection.South => _animConfig.FrontIdleClip,
                _ => _animConfig.SideIdleClip,
            };
        }
        else if (movementState == MovementState.Pursuing || movementState == MovementState.Knockback || movementState == MovementState.Moving)
        {
            clip = cardinalDirection switch
            {
                CardinalDirection.North => _animConfig.BackMoveClip,
                CardinalDirection.South => _animConfig.FrontMoveClip,
                _ => _animConfig.SideMoveClip,
            };
        }

        if (clip != null)
        {
            AnimStateManager.ChangeAnimationState(_networkAnimator.Animator, clip);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateSpriteOrientationClientRpc(CardinalDirection direction)
    {
        // Default scale facing East
        transform.localScale = Vector3.one;

        // Flip sprite for West direction
        if (direction == CardinalDirection.West)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}