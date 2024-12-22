using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkComponent : NetworkBehaviour
{
	public override void OnNetworkSpawn()
	{
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility += CheckVisibility;
			NetworkManager.NetworkTickSystem.Tick += HandleNetworkTick;
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
		var nonClientIdPlayerEnvironment = nonClientIdPlayerObject.GetComponent<Player>().GetPlayerEnvironment();
		var ownerClientEnvironment = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<Player>().GetPlayerEnvironment();
		
		return ownerClientEnvironment == nonClientIdPlayerEnvironment;
	}

	private void HandleNetworkTick()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			if(clientId == OwnerClientId) continue;
			
			// Now testing non owner client's ids
			var shouldBeVisible = CheckVisibility(clientId);
			var isVisibile = NetworkObjectVisibleTo(clientId);
			
			if(shouldBeVisible && !isVisibile)
			{
				Debug.Log($"Showing {clientId}'s player from {OwnerClientId}");
				if(OwnerClientId == NetworkManager.ServerClientId)
				{
					NetworkManager.ConnectedClients[clientId].PlayerObject.gameObject.SetActive(true);
				}
				
				NetworkObject.NetworkShow(clientId);
			}
			else if(!shouldBeVisible && isVisibile)
			{
				Debug.Log($"Hiding {clientId}'s player from {OwnerClientId}");
				if(OwnerClientId == NetworkManager.ServerClientId)
				{
					NetworkManager.ConnectedClients[clientId].PlayerObject.gameObject.SetActive(false);
				}
				
				NetworkObject.NetworkHide(clientId);
			}
		}
	}
	
	private bool NetworkObjectVisibleTo(ulong clientId)
	{
		if(OwnerClientId == NetworkManager.ServerClientId)
		{
			return NetworkManager.ConnectedClients[clientId].PlayerObject.gameObject.activeInHierarchy;
		}
		else
		{
			return NetworkObject.IsNetworkVisibleTo(clientId);
		}
	}

	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility -= CheckVisibility;
			NetworkManager.NetworkTickSystem.Tick -= HandleNetworkTick;
		}

		base.OnNetworkDespawn();
	}
}
