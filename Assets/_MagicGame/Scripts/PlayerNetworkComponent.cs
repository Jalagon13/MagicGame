using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkComponent : NetworkBehaviour
{
	private EnvironmentID _environmentOfExistance;

	public override void OnNetworkSpawn()
	{
		Debug.Log(gameObject.name + OwnerClientId);
		_environmentOfExistance = EnvironmentID.Forest;
	
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility += CheckVisibility;
			NetworkManager.NetworkTickSystem.Tick += HandleNetworkTick;
		}
		base.OnNetworkSpawn();
	}
	
	private bool CheckVisibility(ulong clientId)
	{
		throw new NotImplementedException();
	}

	private void HandleNetworkTick()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			var shouldBeVisibile = CheckVisibility(clientId);
			var isVisibile = NetworkObject.IsNetworkVisibleTo(clientId);
			if (shouldBeVisibile && !isVisibile)
			{
				// Note: This will invoke the CheckVisibility check again
				NetworkObject.NetworkShow(clientId);
			}
			else if (!shouldBeVisibile && isVisibile)
			{
				NetworkObject.NetworkHide(clientId);
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
