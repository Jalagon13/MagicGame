namespace AdvancedTooltips.Core
{
    using System;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Events;

    public class TooltipAnimations : MonoBehaviour
    {
        [SerializeField] private AnimationSettings showUpAnimation;
        [SerializeField] private AnimationSettings hideAnimation;


        public void ShowUpAnimation()
        {
            StopAllCoroutines();

            gameObject.SetActive(true);
            StartCoroutine(RunAnimation(showUpAnimation));
        }
        public void HideAnimation()
        {
            StopAllCoroutines();
            if (!gameObject.activeSelf)
                return;
            StartCoroutine(RunAnimation(hideAnimation));
        }



        public IEnumerator RunAnimation(AnimationSettings settings)
        {
            if (settings.mustBeAtFullScale && !IsAtFullScale())
            {
                gameObject.SetActive(false);
                yield break;
            }

            settings.onAnimationBeginning.Invoke();
            float timer = 0;
            while (timer < settings.animationLength)
            {

                timer += Mathf.Clamp(Time.deltaTime * settings.speedModifier, 0, settings.animationLength);
                transform.localScale = Vector3.one * settings.scaleChangeCurve.Evaluate(timer);
                yield return null;
            }

            settings.onAnimationEnd.Invoke();

        }
        [SerializeField, Range(0, 1)] private float fullScaleMargin = .8f;
        private bool IsAtFullScale()
        {
            return transform.localScale.x > fullScaleMargin;
        }



        [Serializable]
        public class AnimationSettings
        {
            public bool mustBeAtFullScale;
            // you can add more things in here, like position change curve, color, width, height etc.
            public AnimationCurve scaleChangeCurve;
            public float speedModifier = 1;
            public float animationLength = 1;
            public UnityEvent onAnimationBeginning = new();
            public UnityEvent onAnimationEnd = new();
        }
    }
}