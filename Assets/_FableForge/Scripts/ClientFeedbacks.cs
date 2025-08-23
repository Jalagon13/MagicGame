using System;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

public class ClientFeedbacks : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;
    
    [SerializeField]
    private ParticleSystem _gibsParticleSystem;

    private MMF_Player _damageFeedback;
    private MMF_Player _deathFeedback;
    
    private void Awake()
    {
        _damageFeedback = transform.GetChild(0).GetComponent<MMF_Player>();
        _deathFeedback = transform.GetChild(1).GetComponent<MMF_Player>();
    }
    
    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    public void PlayDamageNumbersRpc(int damage)
    {
        GameManager.Instance.PlayDamageNumbers(damage, transform.position, _serverCharacter.CurrentBiome, Color.red);
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    public void PlayDamageFeedbacksRpc()
    {
        SoundManager.Instance.PlayOneShot(_serverCharacter.Data.HurtSound, transform.position);

        _damageFeedback.PlayFeedbacks();
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    public void PlayDeathFeedbacksRpc()
    {
        SoundManager.Instance.PlayOneShot(_serverCharacter.Data.DeathSound, transform.position);
        SoundManager.Instance.PlayOneShot(FMODEvents.Instance.MobSquash, transform.position);

        _deathFeedback.PlayFeedbacks();
    }

    public void RotateGibs(Vector2 hitDirection)
    {
        if (_gibsParticleSystem == null) return;
        if (!float.IsFinite(hitDirection.x) || !float.IsFinite(hitDirection.y)) return;
        
        if (hitDirection == Vector2.zero)
            hitDirection = Vector2.up; // Default direction if none provided

        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        Debug.Log($"RotateGibs: hitDir={hitDirection}, angle={angle}");
        _gibsParticleSystem.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
