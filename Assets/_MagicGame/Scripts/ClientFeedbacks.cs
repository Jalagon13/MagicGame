using System;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

public class ClientFeedbacks : NetworkBehaviour
{
    [SerializeField] 
    private ServerCharacter _serverCharacter;

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
        // SoundManager.Instance.PlayOneShot(_serverCharacter.Data.HurtSound, transform.position);
        SoundManager.Instance.PlayOneShot(_serverCharacter.Data.DeathSound, transform.position);

        _deathFeedback.PlayFeedbacks();
    }
    
    public void PlayDeathSound() // Played through MMF_Player
    {
        // SoundManager.Instance.PlayOneShot(_serverCharacter.Data.WalkSound, transform.position);
        SoundManager.Instance.PlayOneShot(FMODEvents.Instance.MagicDestruction, Player.Instance.transform.position);
    }
}
