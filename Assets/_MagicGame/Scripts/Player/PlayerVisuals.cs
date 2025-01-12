using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerVisuals : NetworkBehaviour
{
    private Player _thisPlayer;

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
	
    public override void OnDestroy()
    {
        _thisPlayer.OnDeath -= Player_OnDeath;
        _thisPlayer.OnRespawn -= Player_OnRespawn;
		
        base.OnDestroy();
    }
}
