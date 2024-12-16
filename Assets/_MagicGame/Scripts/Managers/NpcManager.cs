using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class NpcManager : NetworkBehaviour
{
	public static int SPAWN_ZONE_WIDTH = 48;
	public static int SPAWN_ZONE_HEIGHT = 28;
	public static int NO_SPAWN_ZONE_WIDTH = 35;
	public static int NO_SPAWN_ZONE_HEIGHT = 20;
	
	public static NpcManager Instance { get; private set; }
	
	[SerializeField] private NpcSO _deerNpcSO;
	[SerializeField] private NpcSO _yellowPixieNpcSO;
	[SerializeField] private int _spawnRateDenominator = 600; // Value represents the deniminator of of the spawn rate per tick
	[SerializeField] private float _maxNpcSlotSpawnAmount = 6;
	
	private readonly NetworkList<NetworkObjectReference> _activeNpcNetworkList = new();
	private readonly float _tickTime = 1f / 60f; // 60 ticks per second
	private readonly int _maxSpawnAttempts = 50;
	private float _activeNpcSlotAmount = 0; // Number of active NPCs at a given time,
	private Transform _localPlayerTransform;
	
	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		NetworkManager.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
	}
	
	private void FixedUpdate()
	{
		if(Input.GetKeyDown(KeyCode.G))
		{
			TryToSpawnNpc(Player.LocalClientInstance.transform.position);
		}
	}

	private void NetworkManager_OnClientConnectedCallback(ulong clientId)
	{
		if(NetworkManager.LocalClientId != clientId) return;
		
		_localPlayerTransform = NetworkManager.ConnectedClients[clientId].PlayerObject.transform;
		_activeNpcSlotAmount = 0;
		
		InvokeRepeating(nameof(TrySpawnEntity), 1, _tickTime);
	}
	
	public void TrySpawnEntity()
	{
		// If there is no chunks loaded, then don't try to spawn anything
		// NTFS: This might not work, first place to look if mob spawning is bugged
		if(ChunkManager.Instance.GetLoadedPlayerChunks().Count <= 0) return;
	
		// Calculate the current spawn chance modifier
		float spawnModifier = GetSpawnModifier();
		
		// Adjust spawn rate based on the modifier
		float spawnRate = 1 / (_spawnRateDenominator * spawnModifier);
		
		// Try to spawn an enemy if we're below the max number of NPC slots
		if (_activeNpcSlotAmount < _maxNpcSlotSpawnAmount && UnityEngine.Random.value < spawnRate)
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
					TryToSpawnNpc(potentialSpawnPoint);
					break;
				}
				
				spawnAttempts++;
			}
		}
	}
	
	private void TryToSpawnNpc(Vector2 spawnPosition)
	{
		NpcSO npcToSpawn = NetworkManager.LocalClientId == NetworkManager.ServerClientId ? _deerNpcSO : _yellowPixieNpcSO;
		
		// If there is 'space' to spawn NPC, spawn it
		float remainingNpcSlotSpace = _maxNpcSlotSpawnAmount - _activeNpcSlotAmount;
		
		if(npcToSpawn.SlotAmount <= remainingNpcSlotSpace)
		{
			// Npc to spawn can fit in the remaining npc slot space
			_activeNpcSlotAmount += npcToSpawn.SlotAmount;
			Debug.Log($"[Client {NetworkManager.LocalClientId}] Adding {npcToSpawn.name} slot amount ({npcToSpawn.SlotAmount}) to active npc slots on this client. New amount: {_activeNpcSlotAmount}");
			
			byte npcId = GameManager.Instance.GetIdAsByteFromNpcSO(npcToSpawn);

			SpawnNpcServerRpc(npcId, NetworkManager.LocalClientId, spawnPosition);
		}
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnNpcServerRpc(byte npcId, ulong spawningClientId,Vector2 position)
	{
		NpcSO npcSO = GameManager.Instance.GetNpcSOFromId(npcId);
		
		GameObject npcPrefab = Instantiate(npcSO.Prefab, position, Quaternion.identity);
		
		var npcNetworkComponent = npcPrefab.GetComponent<NpcNetworkComponent>();
		npcNetworkComponent.SetSpawningClientId(spawningClientId);
		npcNetworkComponent.SetNpcId(npcId);
		
		NetworkObject npcPrefabNetworkObject = npcPrefab.GetComponent<NetworkObject>();
		npcPrefabNetworkObject.Spawn(true);
		
		_activeNpcNetworkList.Add(npcPrefabNetworkObject);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void DespawnNpcServerRpc(byte npcId, NetworkObjectReference npcToRemoveNetworkObjectReference, ulong spawningClientId, bool killNpc)
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Removing NPC from active npc list");
			
		_activeNpcNetworkList.Remove(npcToRemoveNetworkObjectReference);
		
		// Either kill or despawn npc depending on the conditional
		npcToRemoveNetworkObjectReference.TryGet(out NetworkObject npcNetworkObject);
		Npc npc = npcNetworkObject.GetComponent<Npc>();
		
		if(killNpc)
		{
			// NTFS: Handle other death stuff here
			npc.DropLoot();
		}
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Destroying Npc from game");
		npc.DestroySelf();
		
		UpdateActiveNpcSlotAmountClientRpc(npcId, RpcTarget.Single(spawningClientId, RpcTargetUse.Persistent));
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void UpdateActiveNpcSlotAmountClientRpc(byte npcId, RpcParams rpcParams)
	{
		NpcSO npc = GameManager.Instance.GetNpcSOFromId(npcId);
	
		_activeNpcSlotAmount -= npc.SlotAmount;
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Removing {npc.name} slot amount ({npc.SlotAmount}) from active npc slots on this client. New amount: {_activeNpcSlotAmount}");
	}
	
	private bool SpawnSpotIsValid(Vector2 potentialSpawnPoint)
	{
		if(PointIsInWall(potentialSpawnPoint)) return false;
		
		// If position is not open space, it is invalid
		if(!PointHasRoom(potentialSpawnPoint)) return false;
		
		// If position is in the no-spawn zone (Camera frustum, NOTE: this is not dynamic; does not change if you change the cam frustum), it's invalid
		if(IsPointInRectangle(potentialSpawnPoint, Player.LocalClientInstance.transform.position, NO_SPAWN_ZONE_WIDTH, NO_SPAWN_ZONE_HEIGHT)) return false;
		
		// If point is in the no-spawn zone of any other player, it is invalid
		if(NetworkManager.ConnectedClientsList.Count > 1)
		{
			foreach (var clientId in NetworkManager.ConnectedClientsIds)
			{
				if(clientId == NetworkManager.LocalClientId) continue;
			
				var otherPlayerPosition = NetworkManager.ConnectedClients[clientId].PlayerObject.transform.position;
			
				if(IsPointInRectangle(potentialSpawnPoint, otherPlayerPosition, NO_SPAWN_ZONE_WIDTH, NO_SPAWN_ZONE_HEIGHT)) return false;
			}
		}
		
		return true;
	}

	private bool PointIsInWall(Vector2 point)
	{
		Vector3Int tilePos = new(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
		
		return Environment.Instance.GetWallTilemapData().GetTilemap().HasTile(tilePos);
	}

	private bool PointHasRoom(Vector2 potentialSpawnSpot)
	{
		// If wall is here, has no room
		Vector3Int tileCheckPos = new((int)potentialSpawnSpot.x, (int)potentialSpawnSpot.y);
		// if(_wallTmObject.Tilemap.HasTile(tileCheckPos)) return false;

		// If There is some world asset here, it has no room
		Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector2(tileCheckPos.x + 0.5f, tileCheckPos.y + 0.5f), 0.1f);
		foreach(var collider in colliders)
		{
			if(collider.GetComponent<WorldObject>() != null) return false;
		}
		
		// NTFS: Implement this later: If there is a floor here, there is no room (Figure out exact rules for this later)
		
		return true;
	}
	
	private bool IsPointInRectangle(Vector2 point, Vector2 rectCenter, float rectWidth, float rectHeight)
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
		float activeRatio = _activeNpcSlotAmount / _maxNpcSlotSpawnAmount;

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
		
		base.OnDestroy();
	}
}
