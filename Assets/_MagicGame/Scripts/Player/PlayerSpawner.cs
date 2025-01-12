using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
	[SerializeField] private GameObject _playerPrefab;

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void SpawnPlayerServerRpc(ulong clientId)
	{
		Debug.Log("Heyy?");
		// Check if the client already has a player object
		if (NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
		{
			Debug.LogWarning($"Client {clientId} already has a player object. Skipping spawn.");
			return;
		}

		// Instantiate and spawn the player prefab
		GameObject playerInstance = Instantiate(_playerPrefab, new Vector3(128, 128), Quaternion.identity);
		playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

		Debug.Log($"Spawned player for client {clientId} at {playerInstance.transform.position}");
		if(clientId == NetworkManager.ServerClientId)
		{
			LoadEnvironment();
		}
	}
	
	private async void LoadEnvironment()
	{
		if(SaveSystem.Instance.EnvironmentDataExists(EnvironmentID.Forest))
		{
			await SaveSystem.Instance.DeserializeAndDispatchData(EnvironmentID.Forest);
		}
		else
		{
			WorldManager.Instance.GenerateEnvironment(EnvironmentID.Forest);
		}
	}
}
