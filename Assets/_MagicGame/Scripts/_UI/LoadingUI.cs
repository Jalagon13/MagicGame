using System;
using UnityEngine;

namespace ProjectWizard
{
    public class LoadingUI : MonoBehaviour
    {
        private void Start()
        {
            GameWorld.Instance.OnBiomeTransitionStart += Show;
            GameWorld.Instance.OnBiomeTransitionEnd += Hide;

            gameObject.SetActive(true);
        }

        private void Show(object sender, EventArgs e)
        {
            gameObject.SetActive(true);
        }

        private void Hide(object sender, EventArgs e)
        {
            Lightmap.Instance.UpdateLightMap();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            GameWorld.Instance.OnBiomeTransitionStart -= Show;
            GameWorld.Instance.OnBiomeTransitionEnd -= Hide;
        }
    }
}
