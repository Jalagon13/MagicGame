using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor.U2D.Animation;
using UnityEngine;

public class NpcManager : NetworkBehaviour
{
	public static NpcManager Instance { get; private set; }
	public static int OUTER_SPAWN_ZONE_WIDTH = 52; // Outer zone from cam frustum
	public static int OUTER_SPAWN_ZONE_HEIGHT = 32; // Outer zone from cam frustum
	public static int NO_SPAWN_ZONE_WIDTH = 35; // Camera Frustum
	public static int NO_SPAWN_ZONE_HEIGHT = 20; // Camera Frustum
	
	[SerializeField] 
	private bool _enableSpawning = true;
	
	[SerializeField] 
	private float _startSpawnDelay;
	
	[SerializeField] 
	private NpcSpawnData _npcSpawnData;
	
	private readonly float _tickTime = 1f / 60f; // 60 ticks per second
	private readonly int _maxSpawnAttempts = 50;
	private Transform _localPlayerTransform;
	private float _currentNpcCapacity = 0;
	
	private void Awake()
	{ 
		Instance = this;
		
		if(NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
		}
	}

	public override void OnDestroy()
	{
		if(NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback -= NetworkManager_OnClientConnectedCallback;
		}
		
		base.OnDestroy();
	}

    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
	{
		if(NetworkManager.LocalClientId != clientId) return;
		
		_localPlayerTransform = NetworkManager.ConnectedClients[clientId].PlayerObject.transform;
		
		InvokeRepeating(nameof(TryToSpawnNpc), _startSpawnDelay, _tickTime);
	}
	
	public void TryToSpawnNpc()
	{
		if(!_enableSpawning) return;
		if(_localPlayerTransform == null) return;
	
		BiomeSpawnRule spawnRule = _npcSpawnData.GetSpawnRules(Player.Instance.CurrentBiome.Value);
		
		// Check if we're at max capacity
		if (_currentNpcCapacity >= spawnRule.MaxNpcSlotAmount) return;
		
		// Calculate spawn probability per tick (Terraria-style)
		float spawnModifier = GetSpawnModifier();
		float spawnsPerMinute = spawnRule.SpawnsPerMinute;
		
		// Convert spawns per minute to probability per tick
		// If we want X spawns per minute and we tick 60 times per second (3600 times per minute)
		// Then probability per tick = X / 3600 * modifier
		float spawnProbability = (spawnsPerMinute / 3600f) * spawnModifier;
		
		// Roll for spawn attempt
		if (UnityEngine.Random.value < spawnProbability)
		{
			// Try to find a valid spawn spot (Terraria-style: limited attempts per tick)
			for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++)
			{
				Vector2 potentialSpawnPoint = GetRandomTileInSpawnArea(); 
				
				if(SpawnSpotIsValid(potentialSpawnPoint))
				{
					float remainingNpcSlotSpace = spawnRule.MaxNpcSlotAmount - _currentNpcCapacity;
					CharacterSpawnData npcToSpawn = _npcSpawnData.SelectRandomNpc(spawnRule.Biome);
					
					if(npcToSpawn.CharacterData.SlotAmount <= remainingNpcSlotSpace)
					{
						SpawnNpc(potentialSpawnPoint, npcToSpawn.CharacterData);
						return; // Successfully spawned, exit
					}
				}
			}
			// If we get here, we rolled for a spawn but couldn't find a valid spot
			// This is normal in Terraria - spawns can fail due to invalid terrain
		}
	}
	
	public void SpawnNpc(Vector2 spawnPosition, CharacterDataSO npcData)
	{
		_currentNpcCapacity += npcData.SlotAmount;
		ushort id = GameDataRegistry.Instance.GetCharacterIdFromCharacterData(npcData);
		SpawnNpcServerRpc(Player.Instance.CurrentBiome.Value, id, NetworkManager.LocalClientId, spawnPosition, npcData.SlotAmount);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnNpcServerRpc(BiomeType spawnBiome, ushort npcId, ulong spawnPlayerId, Vector2 position, float slotAmount)
	{
		CharacterDataSO npcData = GameDataRegistry.Instance.GetCharacterDataFromCharacterId(npcId);
		
		var spawnPosition = new Vector2(Mathf.FloorToInt(position.x) + 0.5f, Mathf.FloorToInt(position.y) + 0.5f);
		GameObject npcPrefab = Instantiate(npcData.NpcPrefab.gameObject, spawnPosition, Quaternion.identity);
		
		NetworkObject npcPrefabNetworkObject = npcPrefab.GetComponent<NetworkObject>();
		npcPrefabNetworkObject.SpawnWithObservers = false;
		npcPrefabNetworkObject.Spawn();

		var npcNetworkComponent = npcPrefab.GetComponent<NpcNetworkVisibility>();
		npcNetworkComponent.InitialieNpcNetwork(spawnPlayerId, npcId, spawnBiome);
	}

	[Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
    public void DecrementNpcSlotsClientRpc(float slotAmount, RpcParams rpcParams = default)
    {
        _currentNpcCapacity -= slotAmount;
    }

    private bool SpawnSpotIsValid(Vector2 potentialSpawnPoint)
	{
		if(PointIsInWall(potentialSpawnPoint)) return false;
		
		if(!IsClear(potentialSpawnPoint)) return false;
		
		if(!TileManager.Instance.TerrainTileRenderer.HasTile(new Vector3Int(Mathf.FloorToInt(potentialSpawnPoint.x), Mathf.FloorToInt(potentialSpawnPoint.y), 0))) return false;
		
		// If point is in the no-spawn zone of any other player, it is invalid (Camera frustum, NOTE: this is not dynamic; does not change if you change the cam frustum)
		if(PointInNoSpawnZoneOfAnyOtherPlayer(potentialSpawnPoint)) return false;
		
		return true;
	}

	private bool IsClear(Vector2 position)
	{
		Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
		var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);

		foreach (Collider2D col in colliders)
		{
			if (col.TryGetComponent(out ResourceObject rsc) || col.TryGetComponent(out NpcNetworkVisibility npc))
				return false;
		}

		return true;
	}

	private bool PointInNoSpawnZoneOfAnyOtherPlayer(Vector2 potentialSpawnPoint)
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			var otherPlayerPosition = NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position;
			
			if(PointInRectangle(potentialSpawnPoint, otherPlayerPosition, NO_SPAWN_ZONE_WIDTH, NO_SPAWN_ZONE_HEIGHT)) return true;
		}
		
		return false;
	}

	private bool PointIsInWall(Vector2 point)
	{
		Vector3Int tilePos = new(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
		
		return TileManager.Instance.WallTm.HasTile(tilePos);
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
		// Define the spawn bounds around the player
		float width = OUTER_SPAWN_ZONE_WIDTH - 1.25f; // Full width of the spawn rectangle minus a little bit so the entity does not spawn out of bounds
		float height = OUTER_SPAWN_ZONE_HEIGHT - 1.25f; // Full height of the spawn rectangle minus a little bit so the entity does not spawn out of bounds

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
		float activeRatio = _currentNpcCapacity / _npcSpawnData.GetSpawnRules(Player.Instance.CurrentBiome.Value).MaxNpcSlotAmount;

		// Terraria-style: More mobs = lower spawn rate, fewer mobs = higher spawn rate
		if (activeRatio < 0.2f)
		{
			return 1.5f; // 50% faster when area is mostly empty
		}
		else if (activeRatio < 0.4f)
		{
			return 1.3f; // 30% faster when area is 20-40% full
		}
		else if (activeRatio < 0.6f)
		{
			return 1.1f; // 10% faster when area is 40-60% full
		}
		else if (activeRatio < 0.8f)
		{
			return 0.9f; // 10% slower when area is 60-80% full
		}
		else if (activeRatio < 0.95f)
		{
			return 0.5f; // 50% slower when area is 80-95% full
		}

		return 0.1f; // 90% slower when area is nearly full
	}
}
