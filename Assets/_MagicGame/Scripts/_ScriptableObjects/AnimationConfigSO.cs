using System;
using UnityEngine;

namespace ProjectWizard
{
    [Serializable]
    public class AnimationConfigSO
    {
        public AnimationClip SideMoveClip;
        public AnimationClip SideIdleClip;
        public AnimationClip FrontMoveClip;
        public AnimationClip FrontIdleClip;
        public AnimationClip BackMoveClip;
        public AnimationClip BackIdleClip;
    }
}
