using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpellNetworkComponent : NetworkBehaviour
{
	public BiomeType SpellBiomeType { get; private set; }

	private GameObject _spellGameObject;
	private ulong _sourcePlayerId;
	private ulong _projectileId;
	private Collider2D _spellCollider;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			_spellGameObject = transform.GetChild(0).gameObject;
			_spellCollider = GetComponent<Collider2D>();
		
			NetworkManager.NetworkTickSystem.Tick += SpellNetworkTick;
		}
		base.OnNetworkSpawn();
	}
	
	public void InitializeSpell(BiomeType biome, ulong sourcePlayerId, float lifetime, ulong projectileId)
	{
		SpellBiomeType = biome;
		_sourcePlayerId = sourcePlayerId;
		_projectileId = projectileId;
		
		// Find walldetectorcollider and populate it
		var wallDetectorCollider = GetComponentInChildren<WallDetectorCollider>();
		if(wallDetectorCollider != null)
		{
			wallDetectorCollider.SetEnvironment(biome, Pathfinding.Instance.GetExistingPathfindingBiomes());
		}
		
		Invoke(nameof(StopProjectile), lifetime);
	}
	
	public void StopProjectile()
	{
		if(NetworkObject.IsSpawned)
		{
			GameManager.Instance.DestroyFakeProjectile(_sourcePlayerId, _projectileId);
			NetworkObject.Despawn();
			Destroy(gameObject);
		}
	}

	private void SpellNetworkTick()
	{
		HandleBiomeVisibility();
	}

	private void HandleBiomeVisibility()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			var isInSameEnvironment = CheckIfInSameEnvironment(clientId);
			var isVisibile = NetworkObjectVisibleTo(clientId);
			
			if(isInSameEnvironment && !isVisibile)
			{
				ShowSpell(clientId);
			}
			else if(!isInSameEnvironment && isVisibile)
			{
				HideSpell(clientId);
			}
		}
	}
	
	private bool CheckIfInSameEnvironment(ulong clientId)
	{
		return NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().CurrentBiome.Value == SpellBiomeType;
	}
	
	private bool NetworkObjectVisibleTo(ulong clientId)
	{
		return clientId == NetworkManager.ServerClientId ? _spellGameObject.activeInHierarchy : NetworkObject.IsNetworkVisibleTo(clientId);
	}

	private void ShowSpell(ulong clientId)
	{
		if(clientId == _sourcePlayerId) return; // Do this because source player should only see the fake projectile
	
		if(clientId == NetworkManager.ServerClientId)
		{
			_spellGameObject.SetActive(true);
			if(_spellCollider != null)
			{
				_spellCollider.enabled = true;
			}
		}
				
		NetworkObject.NetworkShow(clientId);
	}

	private void HideSpell(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_spellGameObject.SetActive(false);
			_spellCollider.enabled = false;
		}
				
		NetworkObject.NetworkHide(clientId);
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			NetworkManager.NetworkTickSystem.Tick -= SpellNetworkTick;
		}

		base.OnNetworkDespawn();
	}
}
