using System;
using MoreMountains.Feedbacks;
using UnityEngine;

public class DamagedFeedback : MonoBehaviour
{
    private DamageReceiver _damageReceiver;
    private MMF_Player _damagedFeedback;
    
    private void Awake()
    {
        _damagedFeedback = GetComponent<MMF_Player>();
        _damageReceiver = transform.root.gameObject.GetComponent<DamageReceiver>();
        if(_damageReceiver == null)
        {
            Debug.LogError($"Npc script not found on root game object");
        }
        _damageReceiver.HpReceived += PlayDamageFeedbacks;
    }

    private void PlayDamageFeedbacks(object sender, DamageReceiver.DamageReceivedEventArgs e)
    {
        _damagedFeedback.PlayFeedbacks();
    }
    
    private void OnDestroy()
    {
        _damageReceiver.HpReceived -= PlayDamageFeedbacks;
    }
}
