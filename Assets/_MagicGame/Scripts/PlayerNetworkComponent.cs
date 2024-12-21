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
		
		return NetworkManager.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<Player>().GetPlayerEnvironment() == NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().GetPlayerEnvironment();
	}

	private void HandleNetworkTick()
	{
		var ownerClientEnvironment = NetworkManager.ConnectedClients[OwnerClientId].PlayerObject.GetComponent<Player>().GetPlayerEnvironment();
		
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			if(clientId == OwnerClientId) continue;
			
			// Now testing non owner client's ids
			var nonClientIdPlayerEnvironment = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().GetPlayerEnvironment();
			
			var isInSameEnvironment = ownerClientEnvironment == nonClientIdPlayerEnvironment;
			var isVisibile = NetworkManager.ConnectedClients[clientId].PlayerObject.IsNetworkVisibleTo(OwnerClientId);
			
			if(isInSameEnvironment && !isVisibile)
			{
				Debug.Log($"Showing {clientId}'s player from {OwnerClientId}");
				if(OwnerClientId == NetworkManager.ServerClientId)
				{
					NetworkManager.ConnectedClients[clientId].PlayerObject.gameObject.SetActive(true);
				}
				
				NetworkManager.ConnectedClients[clientId].PlayerObject.NetworkShow(OwnerClientId);
			}
			else if(!isInSameEnvironment && isVisibile)
			{
				Debug.Log($"Hiding {clientId}'s player from {OwnerClientId}");
				if(OwnerClientId == NetworkManager.ServerClientId)
				{
					NetworkManager.ConnectedClients[clientId].PlayerObject.gameObject.SetActive(false);
				}
				
				NetworkManager.ConnectedClients[clientId].PlayerObject.NetworkHide(OwnerClientId);
			}
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
