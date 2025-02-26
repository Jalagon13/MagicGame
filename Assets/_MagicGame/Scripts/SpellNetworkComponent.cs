using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpellNetworkComponent : NetworkBehaviour
{
	public BiomeType SpellBiomeType { get; private set; }

	private GameObject _spellGameObject;
	private ulong _spawnPlayerId;
	private ulong _projectileId;
	private Collider2D _spellCollider;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			_spellGameObject = transform.GetChild(0).gameObject;
			_spellCollider = GetComponent<Collider2D>();
			
			HideSpell(NetworkManager.ServerClientId);
			
			NetworkObject.CheckObjectVisibility += CheckIfInSameBiome;
			NetworkManager.NetworkTickSystem.Tick += SpellNetworkTick;
		}
		
		base.OnNetworkSpawn();
	}
	
	public void InitializeSpellNetwork(BiomeType biome, ulong spawnPlayerId, float lifetime, ulong projectileId)
	{
		SpellBiomeType = biome;
		_spawnPlayerId = spawnPlayerId;
		_projectileId = projectileId;
		
		if(_spawnPlayerId != NetworkManager.ServerClientId)
		{
			NetworkObject.NetworkHide(_spawnPlayerId);
		}
		
		// Find walldetectorcollider and populate it
		var wallDetectorCollider = GetComponentInChildren<WallColliderDetector>();
		if(wallDetectorCollider != null)
		{
			wallDetectorCollider.SetEnvironment(biome, Pathfinding.Instance.GetExistingPathfindingBiomes());
		}
		
		Invoke(nameof(StopProjectile), lifetime);
	}
	
	public void StopProjectile()
	{
		GameManager.Instance.DestroyLocalProjectile(_spawnPlayerId, _projectileId);
		
		if(NetworkObject.IsSpawned)
		{
			NetworkObject.Despawn();
		}
		
		Destroy(gameObject);
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
	
	private bool CheckIfInSameBiome(ulong clientId)
	{
		return NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().CurrentPlayerBiome.Value == SpellBiomeType;
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
			_spellCollider.enabled = true;
			_spellCollider.isTrigger = true;
		}
		else
		{
			if(clientId == _spawnPlayerId) return; // Do this because source player should only see the fake projectile
		
			NetworkObject.NetworkShow(clientId);
		}
	}

	private void HideSpell(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_spellGameObject.SetActive(false);
			_spellCollider.enabled = false;		
			_spellCollider.isTrigger = false;
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
			NetworkObject.CheckObjectVisibility -= CheckIfInSameBiome;
			NetworkManager.NetworkTickSystem.Tick -= SpellNetworkTick;
		}

		base.OnNetworkDespawn();
	}
}
