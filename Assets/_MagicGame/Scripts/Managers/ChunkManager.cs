using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class ChunkManager : NetworkBehaviour
{
	public static bool IS_GENERATING_BIOME;
	public static int BIOME_SICE_LENGTH = 256;
	public static int CHUNK_SIZE = 4;

	public class OnActiveChunksUpdatedEventArgs : EventArgs
	{
		public Vector2Int MinLoadedTilePos;
		public Vector2Int MaxLoadedTilePos;
	}
	public event EventHandler<ChunkEventArgs> OnLoadChunk;
	public event EventHandler<ChunkEventArgs> OnUnloadChunk;
	public class ChunkEventArgs : EventArgs
	{
		public ChunkGameData Chunk;
	}

	public static ChunkManager Instance { get; private set; }
	
	public Vector2Int MinLoadedTilePosition { get; private set; }
	public Vector2Int MaxLoadedTilePosition { get; private set; }
	
	[SerializeField] private float _chunkLoadCooldown;
	[SerializeField] private int _chunkLoadRadiusX = 5;
	[SerializeField] private int _chunkLoadRadiusY = 4;
	
	private Dictionary<Vector2Int, ChunkGameData> _loadedChunks = new(); // Data structure to hold chunk data that is loaded around player
	private Dictionary<Vector2Int, ChunkGameData> _forestChunks = new(); // Data structure to hold chunk data
	private Dictionary<Vector2Int, ChunkGameData> _caveChunks = new(); // Data structure to hold chunk data
	private Queue<Vector2Int> _chunksToLoad = new Queue<Vector2Int>();
	private Queue<Vector2Int> _chunksToUnload = new Queue<Vector2Int>();
	private ChunkNetworkManager _chunkNetworkManager;
	private Vector2Int _currentChunkPosition; // Current chunk the player is in
	private Vector2Int _lastChunkPosition; // Last chunk position for comparison
	private bool _updateLightsFlag;
	
	private void Awake()
	{
		Instance = this;
		_chunkNetworkManager = GetComponent<ChunkNetworkManager>();
	}
	
	private void Start()
	{
		WorldManager.Instance.OnBiomeDataLoaded += OnBiomeDataLoaded;
		
		InvokeRepeating(nameof(TryToLoadChunk), _chunkLoadCooldown, _chunkLoadCooldown);
	}
	
	private void OnBiomeDataLoaded(object sender, EventArgs e)
	{
		_chunksToLoad.Clear();
		_chunksToUnload.Clear();
	
		_lastChunkPosition = new Vector2Int(-99, 99); // Set it to an impossible chunk position so UpdateChunksAroundPlayer executes;
	}
	
	private void TryToLoadChunk()
	{
		if(Player.LocalClientInstance == null || IS_GENERATING_BIOME || WorldManager.Instance.IsLoadingBiome) return;
		
		Vector2Int newChunkPosition = GetChunkPosition(Player.LocalClientInstance.transform.position);
		if (newChunkPosition != _lastChunkPosition)
		{
			_lastChunkPosition = newChunkPosition;
			_currentChunkPosition = newChunkPosition;
			UpdateChunksAroundPlayer();
		}
	
		if(_chunksToLoad.Count > 0)
		{
			LoadChunk(_chunksToLoad.Dequeue());
		}
			
		if(_chunksToUnload.Count > 0)
		{
			UnloadChunk(_chunksToUnload.Dequeue());
		}
		
		if(_chunksToLoad.Count <= 0 && _chunksToUnload.Count <= 0 && _updateLightsFlag)
		{
			UpdateLightMap();
			_updateLightsFlag = false;
		}
	}
	
	public void UpdateLightMap()
	{
		// Set min and max loaded tile positions by looping through loaded chunks
		Vector2Int minLoadedTilePos = new(int.MaxValue, int.MaxValue);
		Vector2Int maxLoadedTilePos = new(int.MinValue, int.MinValue);

		foreach (var item in _loadedChunks)
		{
			Vector2Int loadedChunkWorldPosition = item.Key * CHUNK_SIZE;
			minLoadedTilePos = Vector2Int.Min(minLoadedTilePos, loadedChunkWorldPosition);
			maxLoadedTilePos = Vector2Int.Max(maxLoadedTilePos, loadedChunkWorldPosition);
		}

		// Add chunk size to maxLoadedTilePosCoord to account for the chunk's area
		maxLoadedTilePos += new Vector2Int(CHUNK_SIZE, CHUNK_SIZE);

		// Set the final values
		MinLoadedTilePosition = minLoadedTilePos;
		MaxLoadedTilePosition = maxLoadedTilePos;
		
		Lightmap.Instance.UpdateLightMap(minLoadedTilePos, maxLoadedTilePos);
	}
	
	public void UpdateChunksAroundPlayer()
	{
		var playerChunkPos = GetChunkPosition(Player.LocalClientInstance.transform.position);

		// Get chunks around the player the player wants to load
		List<Vector2Int> chunksToLoadAroundPlayer = GetPositionsToLoadAroundPlayer();

		// Sort chunks by distance to the player (closest first)
		chunksToLoadAroundPlayer = chunksToLoadAroundPlayer
			.OrderBy(chunkPos => Vector2Int.Distance(playerChunkPos, chunkPos))
			.ToList();

		// For each of those chunks, load them if they are not already loaded
		foreach (Vector2Int chunkPos in chunksToLoadAroundPlayer)
		{
			if (!_loadedChunks.ContainsKey(chunkPos))
			{
				_chunksToLoad.Enqueue(chunkPos);
			}
		}

		// In the loaded player chunks, if any of them are not in chunksToLoadAroundPlayer, unload them
		foreach (Vector2Int chunkPos in _loadedChunks.Keys.ToList())
		{
			if (!chunksToLoadAroundPlayer.Contains(chunkPos))
			{
				_chunksToUnload.Enqueue(chunkPos);
			}
		}
		
		_updateLightsFlag = true;
	}

	private void LoadChunk(Vector2Int chunkPos)
	{
		_chunkNetworkManager.RequestChunkData(Player.LocalClientInstance.CurrentBiome.Value, chunkPos);
	}

	private void UnloadChunk(Vector2Int chunkPos)
	{
		if(_loadedChunks.ContainsKey(chunkPos))
		{
			InvokeOnUnloadChunk(_loadedChunks[chunkPos]);
		}
	}

	public void InvokeOnLoadChunk(ChunkGameData chunkGameDataToLoad)
	{
		if (!_loadedChunks.ContainsKey(chunkGameDataToLoad.ChunkPosition))
		{
			OnLoadChunk?.Invoke(this, new ChunkEventArgs
			{
				Chunk = chunkGameDataToLoad
			});
		
			_loadedChunks.Add(chunkGameDataToLoad.ChunkPosition, chunkGameDataToLoad);
		}
	}
	
	public void InvokeOnUnloadChunk(ChunkGameData chunkGameDataToUnload)
	{
		// Remove the chunk from the list of loaded chunks
		if(_loadedChunks.ContainsKey(chunkGameDataToUnload.ChunkPosition))
		{
			OnUnloadChunk?.Invoke(this, new ChunkEventArgs
			{
				Chunk = chunkGameDataToUnload
			});
		
			_loadedChunks.Remove(chunkGameDataToUnload.ChunkPosition);
		}
	}
	
	public ChunkGameData GetChunkFromChunkPosition(BiomeType environment, Vector2Int chunkPosition)
	{
		switch(environment)
		{
			case BiomeType.Forest:
			
				if(_forestChunks[chunkPosition] == null)
				{
					Debug.LogError($"This should not be playing chunks should exist on requested");
					return null;
				}
				
				return _forestChunks[chunkPosition];
			case BiomeType.Cave:
			
				if(_caveChunks[chunkPosition] == null)
				{
					Debug.LogError($"This should not be playing chunks should exist on requested");
					return null;
				}
				
				return _caveChunks[chunkPosition];
		}
		
		Debug.LogError("No Environment found for _activeEnvironment variable");
		return null;
	}
	
	public List<Vector2Int> GetPositionsToLoadAroundPlayer()
	{
		List<Vector2Int> chunksToLoad = new();
		
		for (int x = -_chunkLoadRadiusX; x <= _chunkLoadRadiusX; x++)
		{
			for (int y = -_chunkLoadRadiusY; y <= _chunkLoadRadiusY; y++)
			{
				Vector2Int chunkCoord = new Vector2Int(_currentChunkPosition.x + x, _currentChunkPosition.y + y);
				chunksToLoad.Add(chunkCoord);
			}
		}
		
		return chunksToLoad;
	}
	
	private Vector2Int GetChunkPosition(Vector3 worldPosition)
	{
		int chunkSize = CHUNK_SIZE;
		return new Vector2Int(
			Mathf.FloorToInt(worldPosition.x / chunkSize),
			Mathf.FloorToInt(worldPosition.y / chunkSize)
		);
	}
	
	public void UnloadAllChunks()
	{
		for (int i = _loadedChunks.Count - 1; i >= 0; i--)
		{
			var chunk = _loadedChunks.ElementAt(i);
			InvokeOnUnloadChunk(chunk.Value);
		}
	}
	
	public bool ToggleDoor(Vector2Int doorPos, BiomeType biome)
	{
		if(!IsServer) return false;
		
		ChunkGameData chunk = GetChunkFromAnyWorldPos(doorPos, biome);
		
		foreach (WorldObjectGameData worldObject in chunk.WorldObjectGameDataList)
		{
			if(worldObject.Position == doorPos)
			{
				// Found door
				var doorObject = worldObject as DoorObjectGameData;
				doorObject.ToggleDoor();
				
				return doorObject.IsOpen;
			}
		}
		
		return false;
	}
	
	public void AddObjectDataToChunk(WorldObjectFileData worldObjectFileData, BiomeType biome, WorldObject worldObject)
	{
		if(!IsServer) return;
		
		ChunkGameData chunk = GetChunkFromAnyWorldPos(worldObjectFileData.Pos, biome);
		
		chunk.AddObjectData(worldObjectFileData, worldObject);
	}
	
	public void AddObjectDataToChunk(Vector2Int position, WorldObject worldObject, BiomeType biomeToPlaceIn)
	{
		if(!IsServer) return;
		
		ChunkGameData chunk = GetChunkFromAnyWorldPos(position, biomeToPlaceIn);
		chunk.AddObjectData(position, worldObject);
	}
	
	public void RemoveObjectDataFromChunk(Vector2Int position, BiomeType biomeToRemoveFrom)
	{
		if(!IsServer) return;
		
		GetChunkFromAnyWorldPos(position, biomeToRemoveFrom).RemoveObjectData(position);
		TryToRemoveObjectClientRpc(position, biomeToRemoveFrom);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void TryToRemoveObjectClientRpc(Vector2Int position, BiomeType biomeToRemoveObjData)
	{
		if(Player.LocalClientInstance.CurrentBiome.Value != biomeToRemoveObjData || !ObjectPositionInLoadedChunks(position)) return;
		
		if(ObjectManager.Instance.TryToFindWorldObject(position, out WorldObject wo))
		{
			wo.DestroySelf();
		}
	}
	
	public void AddWallTileDataToChunk(Vector2Int position, int tileID, BiomeType biomeToAddTileData, TileType tileType)
	{
		if(!IsServer) return;
	
		ChunkGameData chunk = GetChunkFromAnyWorldPos(position, biomeToAddTileData);
		chunk.AddWallTileData(position, GameManager.Instance.GetTileSOFromID(tileID));
		HandleTileVisualClientRpc((Vector3Int)position, tileID, tileType, biomeToAddTileData);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void HandleTileVisualClientRpc(Vector3Int pos, int syncTileId, TileType syncTileType, BiomeType biome)
	{
		if(Player.LocalClientInstance.CurrentBiome.Value != biome || !ObjectPositionInLoadedChunks((Vector2Int)pos)) return;
		
		TileSO tileToPlace = GameManager.Instance.GetTileSOFromID(syncTileId);

		// Chunk is loaded visually, therefore visually update whatever tile wants to be updated
		switch(syncTileType)
		{
			case TileType.Ground:
				break;
			case TileType.Floor:
				Environment.Instance.FloorTm.SetTile(pos, tileToPlace);
				break;
			case TileType.Wall:
				Environment.Instance.WallTm.SetTile(pos, tileToPlace);
				Environment.Instance.TileVisibilityDict[pos] = new TileVisibility {Visibility = 1};
				Lightmap.Instance.UpdateLightMap();
				break;
		}
	}
	
	public void RemoveWallTileDataFromChunk(Vector2Int position, BiomeType biomeToRemoveTileData)
	{
		if(!IsServer) return;
	
		GetChunkFromAnyWorldPos(position, biomeToRemoveTileData).RemoveWallTileData(position);
		TryToRemoveWallTileClientRpc(position, biomeToRemoveTileData);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void TryToRemoveWallTileClientRpc(Vector2Int position, BiomeType biomeToRemoveTileData)
	{
		if(Player.LocalClientInstance.CurrentBiome.Value != biomeToRemoveTileData || !ObjectPositionInLoadedChunks(position)) return;
		
		Environment.Instance.WallTm.SetTile((Vector3Int)position, null);
	}
	
	public bool ObjectPositionInLoadedChunks(Vector2Int position) // Check if the position is within the bounds
	{
		return position.x >= MinLoadedTilePosition.x && position.x <= MaxLoadedTilePosition.x &&
			   position.y >= MinLoadedTilePosition.y && position.y <= MaxLoadedTilePosition.y;
	}

	public ChunkGameData GetChunkFromAnyWorldPos(Vector2Int anyWorldPos, BiomeType environmentToGetChunkFrom)
	{
		Vector2Int chunkCoord = GetChunkCoordFromPosition(anyWorldPos);
		
		var chunks = GetChunksFromBiome(environmentToGetChunkFrom);
		chunks.TryGetValue(chunkCoord, out ChunkGameData chunk);
		
		return chunk;
	}
	
	private Vector2Int GetChunkCoordFromPosition(Vector2 position)
	{
		int chunkX = Mathf.FloorToInt(position.x / CHUNK_SIZE);
		int chunkY = Mathf.FloorToInt(position.y / CHUNK_SIZE);
		return new Vector2Int(chunkX, chunkY);
	}

	public Dictionary<Vector2Int, ChunkGameData> GetChunksFromBiome(BiomeType environmentToGet)
	{
		switch(environmentToGet)
		{
			case BiomeType.Forest:
				return _forestChunks;
			case BiomeType.Cave:
				return _caveChunks;
		}
		
		Debug.LogError($"Environment {environmentToGet} should exist but doesn't, add environment chunks to ChunkManager");
		return null;
	}
	

	public void LoadChunksForBiome(BiomeType biomeToSetChunksFor, Dictionary<Vector2Int, ChunkGameData> newChunks)
	{
		switch(biomeToSetChunksFor)
		{
			case BiomeType.Forest:
				_forestChunks = newChunks;
				return;
			case BiomeType.Cave:
				_caveChunks = newChunks;
				return;
		}
		
		Debug.LogError("No Environment found for _activeEnvironment variable");
	}
	
	
	private Vector3Int GetPlayerTilePos()
	{
		int xPos = Mathf.FloorToInt(Player.LocalClientInstance.transform.position.x);
		int yPos = Mathf.FloorToInt(Player.LocalClientInstance.transform.position.y);
		
		return new(xPos, yPos);
	}
	
	public Dictionary<Vector2Int, ChunkGameData> GetLoadedPlayerChunks()
	{
		return _loadedChunks;
	}
}
