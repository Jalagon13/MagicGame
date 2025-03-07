using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerVisuals : NetworkBehaviour
{
    private Player _thisPlayer;
    private GameObject _chargeVfx;
    

    private void Awake()
    {
        _thisPlayer = transform.root.GetComponent<Player>();

        _thisPlayer.OnDeath += Player_OnDeath;
        _thisPlayer.OnRespawn += Player_OnRespawn;
    }

    private void Player_OnDeath(object sender, Player.PlayerIdEventArgs e)
    {
        Hide();
    }

    private void Player_OnRespawn(object sender, Player.PlayerIdEventArgs e)
    {
        Show();
    }
	
    private void Show()
    {
        gameObject.SetActive(true);
    }
	
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    
    [Rpc(SendTo.ClientsAndHost)]
    public void PlayChargeVFXClientRpc(int spellIndex)
    {
        GameObject chargeVfx = (GameManager.Instance.GetItemSOFromItemId(spellIndex) as SpellItemSO).ChargeVFX;
        _chargeVfx = Instantiate(chargeVfx, _thisPlayer.MainHand.ProjectileSpawnTransform);
        _chargeVfx.transform.localPosition = Vector3.zero;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void StopChargeVfxClientRpc()
    {
        var main = _chargeVfx.GetComponent<ParticleSystem>().main;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;
    }
	
    public override void OnDestroy()
    {
        _thisPlayer.OnDeath -= Player_OnDeath;
        _thisPlayer.OnRespawn -= Player_OnRespawn;
		
        base.OnDestroy();
    }
}
