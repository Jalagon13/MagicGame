using System;
using MoreMountains.Feedbacks;
using UnityEngine;

public class DamagedFeedback : MonoBehaviour
{
    private Npc _npc;
    private MMF_Player _damagedFeedback;
    
    private void Awake()
    {
        _damagedFeedback = GetComponent<MMF_Player>();
        _npc = transform.root.gameObject.GetComponent<Npc>();
        if(_npc == null)
        {
            Debug.LogError($"Npc script not found on root game object");
        }
    }

    private void Start()
    {
        _npc.OnClientNpcDamged += PlayFeedbacks;
    }

    private void PlayFeedbacks(object sender, Npc.OnNpcDamagedEventArgs e)
    {
        _damagedFeedback.PlayFeedbacks();
    }
    
    private void OnDestroy()
    {
        _npc.OnClientNpcDamged -= PlayFeedbacks;
    }
}
