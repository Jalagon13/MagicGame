using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public static class AnimStateManager
{
    public static string CurrentState;
    public static int CurrentStateHash;
    public static int CurrentAnimatorInstanceID;

    public static void ChangeAnimationState(Animator animator, AnimationClip animClip)
    {
        Timing.RunCoroutine(ChangeState(animator, animClip));
    }
	
    private static IEnumerator<float> ChangeState(Animator animator, AnimationClip animClip)
    {
        yield return Timing.WaitForOneFrame;
		
        int animHash = Animator.StringToHash(animClip.name);
		
        if (CurrentStateHash == animHash)
        {
            if (animator.GetInstanceID() != CurrentAnimatorInstanceID)
            {
                PlayHashAnimation(animator, animHash);
            }

            yield return 0;
        }
        PlayHashAnimation(animator, animHash);
    }
	
    private static void PlayHashAnimation(Animator animator, int newHashState)
    {
        if(animator == null) return;
	
        if (animator.gameObject.activeInHierarchy)
        {
            animator.Play(newHashState);
            CurrentStateHash = newHashState;
            CurrentAnimatorInstanceID = animator.GetInstanceID();
        }
    }

    private static void PlayAnimation(Animator animator, AnimationClip newState)
    {
        animator.Play(newState.name);
        CurrentState = newState.name;
        CurrentAnimatorInstanceID = animator.GetInstanceID();
    }

    // refactor later down the line
    //public static Animator ChangeAnimationState(Animator animator, int newHashState)
    //{
    //    if (CurrentStateHash == newHashState) return null;

    //    animator.Play(newHashState);
    //    CurrentStateHash = newHashState;

    //    return animator;
    //}
}
