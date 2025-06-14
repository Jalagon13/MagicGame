using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpellNetworkVisibility : NetworkBehaviour
{
	private SyncSpellData _spellData;
	private GameObject _spellGameObject;
	private Collider2D _spellCollider;
	private ServerSpell _spell;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			_spellGameObject = transform.GetChild(0).gameObject;
			_spellCollider = GetComponent<Collider2D>();
			
			NetworkObject.CheckObjectVisibility += InitialVisCheck;
			NetworkManager.NetworkTickSystem.Tick += SpellNetworkTick;
		}
	}

    protected override void OnNetworkPostSpawn()
    {
		if(IsServer)
		{
			HideSpell(NetworkManager.ServerClientId);
		}
	}
	
	public void InitializeSpellNetwork(SyncSpellData syncSpellData)
	{
		_spell = GetComponent<ServerSpell>();
		_spellData = syncSpellData;
	}

	private void SpellNetworkTick()
	{
		HandleBiomeVisibility();
	}

	private void HandleBiomeVisibility()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			var isInSameBiome = CheckIfInSameBiome(clientId);
			var isVisibile = NetworkObjectVisibleTo(clientId);
			
			if(isInSameBiome && !isVisibile)
			{
				ShowSpell(clientId);
			}
			else if(!isInSameBiome && isVisibile)
			{
				HideSpell(clientId);
			}
		}
	}
	
	private bool InitialVisCheck(ulong clientId)
	{
		// if (!_spell.Started) return false;
		
		return CheckIfInSameBiome(clientId);
	}
	
	private bool CheckIfInSameBiome(ulong clientId)
	{
		return NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().CurrentBiome.Value == _spellData.SpawnBiome;
	}
	
	private bool NetworkObjectVisibleTo(ulong clientId)
	{
		return clientId == NetworkManager.ServerClientId ? _spellGameObject.activeInHierarchy : NetworkObject.IsNetworkVisibleTo(clientId);
	}

	private void ShowSpell(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_spellGameObject.SetActive(true);
			if(_spellCollider != null)
			{
				_spellCollider.enabled = true;
				_spellCollider.isTrigger = true;
			}
		}
		else
		{
			// if(clientId == _spellData.SpawnPlayerId) return; // Do this because source player should only see the fake projectile
		
			NetworkObject.NetworkShow(clientId);
		}
	}

	private void HideSpell(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_spellGameObject.SetActive(false);
			if(_spellCollider != null)
			{
				_spellCollider.enabled = false;
				_spellCollider.isTrigger = false;
			}
		}
		else
		{
			NetworkObject.NetworkHide(clientId);
		}
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility -= InitialVisCheck;
			NetworkManager.NetworkTickSystem.Tick -= SpellNetworkTick;
		}

		base.OnNetworkDespawn();
	}
}
