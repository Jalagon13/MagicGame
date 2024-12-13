using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class SpriteAnimationHandler : MonoBehaviour
{
    // Animation Clips for different states and directions
    [FoldoutGroup("Animation Clips")] [SerializeField] private AnimationClip _sideMoveClip;
    [FoldoutGroup("Animation Clips")] [SerializeField] private AnimationClip _sideIdleClip;
    [FoldoutGroup("Animation Clips")] [SerializeField] private AnimationClip _frontMoveClip;
    [FoldoutGroup("Animation Clips")] [SerializeField] private AnimationClip _frontIdleClip;
    [FoldoutGroup("Animation Clips")] [SerializeField] private AnimationClip _backMoveClip;
    [FoldoutGroup("Animation Clips")] [SerializeField] private AnimationClip _backIdleClip;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void PlayIdleAnimation(CardinalDirection direction)
    {
        UpdateSpriteOrientation(direction);
	
        AnimationClip idleClip = direction switch
        {
            CardinalDirection.North => _backIdleClip,
            CardinalDirection.South => _frontIdleClip,
            _ => _sideIdleClip,
        };

        AnimStateManager.ChangeAnimationState(_animator, idleClip);
    }
	
    public void PlayMoveAnimation(CardinalDirection direction)
    {
        UpdateSpriteOrientation(direction);
	
        AnimationClip moveClip = direction switch
        {
            CardinalDirection.North => _backMoveClip,
            CardinalDirection.South => _frontMoveClip,
            _ => _sideMoveClip,
        };

        AnimStateManager.ChangeAnimationState(_animator, moveClip);
    }

    private void UpdateSpriteOrientation(CardinalDirection direction)
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