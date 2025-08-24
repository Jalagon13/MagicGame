using System;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

public class ClientFeedbacks : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;
    
    [SerializeField]
    private ParticleSystem _damagedParticles, _deathParticles;

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
    public void PlayDamageFeedbacksRpc(Vector2 hitDirection)
    {
        SoundManager.Instance.PlayOneShot(_serverCharacter.Data.HurtSound, transform.position);
        RotateFeedbacks(hitDirection);
        _damageFeedback.PlayFeedbacks();
    }

    [Rpc(SendTo.ClientsAndHost, RequireOwnership = false)]
    public void PlayDeathFeedbacksRpc(Vector2 hitDirection)
    {
        SoundManager.Instance.PlayOneShot(_serverCharacter.Data.DeathSound, transform.position);
        SoundManager.Instance.PlayOneShot(FMODEvents.Instance.MobSquash, transform.position);
        RotateFeedbacks(hitDirection);
        _deathFeedback.PlayFeedbacks();
    }

    private void RotateFeedbacks(Vector2 hitDirection)
    {
        if (_deathParticles == null || _damagedParticles == null) 
        {
            Debug.LogWarning($"Either Death or Damaged Particles are null therefore can't be rotated");
        }
        
        if (!float.IsFinite(hitDirection.x) || !float.IsFinite(hitDirection.y)) return;
        
        if (hitDirection == Vector2.zero)
            hitDirection = Vector2.up; // Default direction if none provided

        float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
        // Debug.Log($"RotateGibs: hitDir={hitDirection}, angle={angle}");
        _damagedParticles.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        _deathParticles.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
