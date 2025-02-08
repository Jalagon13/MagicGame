using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NpcManager : NetworkBehaviour
{
	public static NpcManager Instance { get; private set; }
	public static int SPAWN_ZONE_WIDTH = 48; // Outer zone from cam frustum
	public static int SPAWN_ZONE_HEIGHT = 28; // Outer zone from cam frustum
	public static int NO_SPAWN_ZONE_WIDTH = 35; // Camera Frustum
	public static int NO_SPAWN_ZONE_HEIGHT = 20; // Camera Frustum
	
	[field: SerializeField] public Npc TestDummyPrefab { get; private set; }
	[SerializeField] private BiomeSpawnParamsSO _biomeSpawnParamsSO;
	[SerializeField] private bool _enableSpawning = true;
	[SerializeField] private float _startSpawnDelay;
	
	private readonly NetworkList<NetworkObjectReference> _activeNpcNetworkList = new();
	private readonly float _tickTime = 1f / 60f; // 60 ticks per second
	private readonly int _maxSpawnAttempts = 50;
	private float _activeNpcSlotAmount = 0; // Number of active NPCs at a given time,
	private Transform _localPlayerTransform;
	
	private void Awake()
	{
		Instance = this;
		
		if(NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
		}
	}

	private void Start()
	{
		GameInput.Instance.OnResearchMenuButton += GameInput_OnResearchMenuButton;
	}

	private void GameInput_OnResearchMenuButton(object sender, EventArgs e)
	{
		if(!IsServer) return;
		
		SpawnTestDummyServerRpc();
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnTestDummyServerRpc()
	{
		GameObject npcPrefab = Instantiate(TestDummyPrefab.gameObject, ActionManager.MouseWorldPosition, Quaternion.identity);
		
		var npcNetworkComponent = npcPrefab.GetComponent<NpcNetworkComponent>();
		npcNetworkComponent.SetEnvironment(Player.LocalClientInstance.CurrentBiome.Value);
		npcNetworkComponent.SetSpawningClientId(0);
		npcNetworkComponent.SetNpcId(-1);
		
		NetworkObject npcPrefabNetworkObject = npcPrefab.GetComponent<NetworkObject>();
		npcPrefabNetworkObject.Spawn(true);
	}

	private void NetworkManager_OnClientConnectedCallback(ulong clientId)
	{
		if(NetworkManager.LocalClientId != clientId) return;
		
		_localPlayerTransform = NetworkManager.ConnectedClients[clientId].PlayerObject.transform;
		_activeNpcSlotAmount = 0;
		
		InvokeRepeating(nameof(TryToSpawnNpc), _startSpawnDelay, _tickTime);
	}
	
	public void TryToSpawnNpc()
	{
		if(ChunkManager.Instance.GetLoadedPlayerChunks().Count <= 0 || !_enableSpawning) return;
	
		// Calculate the current spawn chance modifier and adjust spawn rate based on the modifier
		float spawnModifier = GetSpawnModifier();
		float spawnRate = 1 / (_biomeSpawnParamsSO.GetCurrentBiomeSpawnRule().SpawnRateDenominator * spawnModifier);
		
		// Try to spawn an enemy if we're below the max number of NPC slots
		if (_activeNpcSlotAmount < _biomeSpawnParamsSO.GetCurrentBiomeSpawnRule().MaxNpcSlotAmount && UnityEngine.Random.value < spawnRate)
		{
			// Try to find a valid spawn spot and spawn an entity on the first one found
			int spawnAttempts = 0;
			
			while(spawnAttempts < _maxSpawnAttempts)
			{
				if(_localPlayerTransform == null)
				{
					break;
				}
				
				Vector2 potentialSpawnPoint = GetRandomTileInSpawnArea(); 
				
				if(SpawnSpotIsValid(potentialSpawnPoint))
				{
					float remainingNpcSlotSpace = _biomeSpawnParamsSO.GetCurrentBiomeSpawnRule().MaxNpcSlotAmount - _activeNpcSlotAmount;
					NpcSpawnData npcToSpawn = _biomeSpawnParamsSO.GetCurrentBiomeSpawnRule().GetRandomNpc();
					
					if(npcToSpawn.SlotAmount <= remainingNpcSlotSpace)
					{
						SpawnNpc(potentialSpawnPoint, npcToSpawn);
						
						break;
					}
				}
				
				spawnAttempts++;
			}
		}
	}
	
	private void SpawnNpc(Vector2 spawnPosition, NpcSpawnData npcSpawnData)
	{
		// Npc to spawn can fit in the remaining npc slot space
		_activeNpcSlotAmount += npcSpawnData.SlotAmount;
			
		int npcId = GameManager.Instance.GetNpcIdFromNpcSpawnData(Player.LocalClientInstance.CurrentBiome.Value, npcSpawnData);

		SpawnNpcServerRpc(Player.LocalClientInstance.CurrentBiome.Value, npcId, NetworkManager.LocalClientId, spawnPosition);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnNpcServerRpc(BiomeType spawnBiome, int npcId, ulong spawningClientId, Vector2 position)
	{
		NpcSpawnData npcSpawnData = GameManager.Instance.GetNpcSpawnData(spawnBiome, npcId);
		
		var spawnPosition = new Vector2(Mathf.FloorToInt(position.x) + 0.5f, Mathf.FloorToInt(position.y) + 0.5f);
		GameObject npcPrefab = Instantiate(npcSpawnData.Prefab, spawnPosition, Quaternion.identity);
		
		var npcNetworkComponent = npcPrefab.GetComponent<NpcNetworkComponent>();
		npcNetworkComponent.SetEnvironment(spawnBiome);
		npcNetworkComponent.SetSpawningClientId(spawningClientId);
		npcNetworkComponent.SetNpcId(npcId);
		
		NetworkObject npcPrefabNetworkObject = npcPrefab.GetComponent<NetworkObject>();
		npcPrefabNetworkObject.Spawn(true);
		
		// replace this with dimension specific entity list that contains mobs and projectiles for entities
		_activeNpcNetworkList.Add(npcPrefabNetworkObject);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void DespawnNpcServerRpc(int npcId, NetworkObjectReference npcToRemoveNetworkObjectReference, ulong spawningClientId, bool killNpc)
	{
		_activeNpcNetworkList.Remove(npcToRemoveNetworkObjectReference);
		
		// Either kill or despawn npc depending on the conditional
		npcToRemoveNetworkObjectReference.TryGet(out NetworkObject npcNetworkObject);
		Npc npc = npcNetworkObject.GetComponent<Npc>();
		NpcNetworkComponent npcNetworkComponent = npc.GetComponent<NpcNetworkComponent>();
		
		if(killNpc)
		{
			// NTFS: Handle other death stuff here
			npc.DropLoot();
		}
		
		npc.DestroySelf();
		
		UpdateActiveNpcSlotAmountClientRpc(npcNetworkComponent.NpcBiomeType, npcId, RpcTarget.Single(spawningClientId, RpcTargetUse.Persistent));
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void UpdateActiveNpcSlotAmountClientRpc(BiomeType biome, int npcId, RpcParams rpcParams)
	{
		NpcSpawnData npc = GameManager.Instance.GetNpcSpawnData(biome, npcId);
	
		_activeNpcSlotAmount -= npc.SlotAmount;
	}
	
	private bool SpawnSpotIsValid(Vector2 potentialSpawnPoint)
	{
		if(PointIsInWall(potentialSpawnPoint)) return false;
		
		// If point is in the no-spawn zone of any other player, it is invalid (Camera frustum, NOTE: this is not dynamic; does not change if you change the cam frustum)
		if(PointInNoSpawnZoneOfAnyOtherPlayer(potentialSpawnPoint)) return false;
		
		return true;
	}
	
	private bool PointInNoSpawnZoneOfAnyOtherPlayer(Vector2 potentialSpawnPoint)
	{
		if(NetworkManager.ConnectedClientsList.Count > 1)
		{
			foreach (var clientId in NetworkManager.ConnectedClientsIds)
			{
				var otherPlayerPosition = NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position;
			
				if(PointInRectangle(potentialSpawnPoint, otherPlayerPosition, NO_SPAWN_ZONE_WIDTH, NO_SPAWN_ZONE_HEIGHT)) return true;
			}
		}
		
		return false;
	}

	private bool PointIsInWall(Vector2 point)
	{
		Vector3Int tilePos = new(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
		
		return Environment.Instance.WallTm.HasTile(tilePos);
	}
	
	private bool PointInRectangle(Vector2 point, Vector2 rectCenter, float rectWidth, float rectHeight)
	{
		// Calculate the bounds of the rectangle
		float halfWidth = rectWidth / 2;
		float halfHeight = rectHeight / 2;

		float minX = rectCenter.x - halfWidth;
		float maxX = rectCenter.x + halfWidth;
		float minY = rectCenter.y - halfHeight;
		float maxY = rectCenter.y + halfHeight;

		// Check if the point is within the bounds
		return point.x >= minX && point.x <= maxX && point.y >= minY && point.y <= maxY;
	}
	
	private Vector2 GetRandomTileInSpawnArea()
	{
		// Set a different random seed every time the game starts
		UnityEngine.Random.InitState(DateTime.Now.Millisecond);

		// Define the spawn bounds around the player
		float width = SPAWN_ZONE_WIDTH - 1.25f; // Full width of the spawn rectangle minus a little bit so the entity does not spawn out of bounds
		float height = SPAWN_ZONE_HEIGHT - 1.25f; // Full height of the spawn rectangle minus a little bit so the entity does not spawn out of bounds

		// Calculate the rectangular bounds
		float minX = _localPlayerTransform.position.x - (width / 2); // Subtract half of the width to center it around the player
		float maxX = _localPlayerTransform.position.x + (width / 2); // Add half of the width to center it around the player
		float minY = _localPlayerTransform.position.y - (height / 2); // Subtract half of the height to center it around the player
		float maxY = _localPlayerTransform.position.y + (height / 2); // Add half of the height to center it around the player

		// Generate a random position within the bounds
		float randomX = UnityEngine.Random.Range(minX, maxX);
		float randomY = UnityEngine.Random.Range(minY, maxY);

		// Round the position to the nearest tile and return the center of the tile
		Vector3Int tilePosition = new(Mathf.RoundToInt(randomX), Mathf.RoundToInt(randomY), 0);
		Vector2 centerTilePosition = new(tilePosition.x + 0.5f, tilePosition.y + 0.5f);

		return centerTilePosition;
	}
	
	private float GetSpawnModifier()
	{
		float activeRatio = _activeNpcSlotAmount / _biomeSpawnParamsSO.GetCurrentBiomeSpawnRule().MaxNpcSlotAmount;

		if (activeRatio < 0.2f)
		{
			return 0.6f;
		}
		else if (activeRatio < 0.4f)
		{
			return 0.7f;
		}
		else if (activeRatio < 0.6f)
		{
			return 0.8f;
		}
		else if (activeRatio < 0.8f)
		{
			return 0.9f;
		}

		return 1f;
	}

	public override void OnDestroy()
	{
		if(NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
		}
		
		GameInput.Instance.OnResearchMenuButton -= GameInput_OnResearchMenuButton;
		
		base.OnDestroy();
	}
}
