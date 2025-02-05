using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkComponent : NetworkBehaviour
{
	private GameObject _playerGameObject;

	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility += CheckVisibility;
			NetworkManager.NetworkTickSystem.Tick += HandleOtherPlayerVisibility;
			
			_playerGameObject = transform.GetChild(0).gameObject;
		}
		base.OnNetworkSpawn();
	}
	
	private bool CheckVisibility(ulong clientId)
	{
		if (!IsSpawned)
		{
			return false;
		}
		
		if(clientId == OwnerClientId) return true;
		
		var nonClientIdPlayerObject = NetworkManager.ConnectedClients[clientId].PlayerObject;
		var nonClientIdPlayerEnvironment = nonClientIdPlayerObject.GetComponent<Player>().CurrentBiome.Value;
		var ownerClientEnvironment = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<Player>().CurrentBiome.Value;
		
		return ownerClientEnvironment == nonClientIdPlayerEnvironment;
	}

	private void HandleOtherPlayerVisibility()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			if(clientId == OwnerClientId) continue;
			
			// Now testing non owner client's ids
			var shouldBeVisible = CheckVisibility(clientId);
			var isVisibile = NetworkObjectVisibleTo(clientId);
			
			if(shouldBeVisible && !isVisibile)
			{
				// Debug.Log($"Showing {clientId}'s player from {OwnerClientId}");
				if(clientId == NetworkManager.ServerClientId)
				{
					_playerGameObject.SetActive(true);
				}
				
				NetworkObject.NetworkShow(clientId);
			}
			else if(!shouldBeVisible && isVisibile)
			{
				// Debug.Log($"Hiding {clientId}'s player from {OwnerClientId}");
				if(clientId == NetworkManager.ServerClientId)
				{
					_playerGameObject.SetActive(false);
				}
				
				NetworkObject.NetworkHide(clientId);
			}
		}
	}
	
	private bool NetworkObjectVisibleTo(ulong clientId)
	{
		return clientId == NetworkManager.ServerClientId ? _playerGameObject.activeInHierarchy : NetworkObject.IsNetworkVisibleTo(clientId);
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility -= CheckVisibility;
			NetworkManager.NetworkTickSystem.Tick -= HandleOtherPlayerVisibility;
		}

		base.OnNetworkDespawn();
	}
}
