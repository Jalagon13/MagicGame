using System;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    private void Start()
    {
        WorldManager.Instance.OnBiomeTransitionStart += Show;
        WorldManager.Instance.OnBiomeTransitionEnd += Hide;
        
        gameObject.SetActive(false);
    }

    private void Show(object sender, EventArgs e)
    {
        gameObject.SetActive(true);
    }

    private void Hide(object sender, EventArgs e)
    {
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        WorldManager.Instance.OnBiomeTransitionStart -= Show;
        WorldManager.Instance.OnBiomeTransitionEnd -= Hide;
    }
}
