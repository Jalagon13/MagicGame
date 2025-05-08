using System;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    private void Start()
    {
        WorldManager.Instance.OnBiomeTransitionStart += Show;
        WorldManager.Instance.OnBiomeTransitionEnd += Hide;
        
        gameObject.SetActive(true);
    }

    private void Show(object sender, EventArgs e)
    {
        gameObject.SetActive(true);
        Debug.Log($"Showing loading UI");
    }

    private void Hide(object sender, EventArgs e)
    {
        gameObject.SetActive(false);
        Debug.Log($"Hiding loading UI");
    }

    private void OnDestroy()
    {
        WorldManager.Instance.OnBiomeTransitionStart -= Show;
        WorldManager.Instance.OnBiomeTransitionEnd -= Hide;
    }
}
