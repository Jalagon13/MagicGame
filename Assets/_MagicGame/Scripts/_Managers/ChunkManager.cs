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
	public static ChunkManager Instance { get; private set; }
	
	public static bool IS_GENERATING_BIOME;
	public static int BIOME_SIDE_LENGTH = 256;
	public static int CHUNK_SIZE = 32;

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
	
	private Dictionary<Vector2Int, ChunkGameData> _forestChunks = new(); // Data structure to hold chunk data
	private Dictionary<Vector2Int, ChunkGameData> _caveChunks = new(); // Data structure to hold chunk data
	private ChunkNetworkManager _chunkNetworkManager;
	private List<ChunkGameData> _chunksToLoad = new();
	
	private void Awake()
	{
		Instance = this;
		
		_chunkNetworkManager = GetComponent<ChunkNetworkManager>();
	}
	
	private void Start()
	{
		WorldManager.Instance.OnBiomeDataLoaded += OnBiomeDataLoaded;
	}
	
	private List<Vector2Int> GetChunkPositions()
	{
		Vector2Int playerChunkPos = GetChunkCoordFromPosition(Player.LocalClientInstance.transform.position);
	    List<Vector2Int> chunkPositions = new();
	    int numChunks = BIOME_SIDE_LENGTH / CHUNK_SIZE;
	
	    for (int y = 0; y < numChunks; y++)
	    {
	        for (int x = 0; x < numChunks; x++)
	        {
	            chunkPositions.Add(new Vector2Int(x, y));
	        }
	    }
	
	    chunkPositions = chunkPositions.OrderBy(pos => Vector2Int.Distance(pos, playerChunkPos)).ToList();
	    return chunkPositions;
	}
	
	private void OnBiomeDataLoaded(object sender, EventArgs e)
	{
		// StartCoroutine(StaggerChunkRequests());
		_chunksToLoad = new List<ChunkGameData>((BIOME_SIDE_LENGTH / CHUNK_SIZE) * (BIOME_SIDE_LENGTH / CHUNK_SIZE));

		foreach (Vector2Int chunkPos in GetChunkPositions())
		{
			_chunkNetworkManager.RequestChunkDataServerRpc(Player.LocalClientInstance.OwnerClientId, Player.LocalClientInstance.CurrentPlayerBiome.Value, chunkPos);
		}

		Debug.Log($"ChunkManager: OnBiomeDataLoaded for {Player.LocalClientInstance.CurrentPlayerBiome.Value}");
	}
	
	private IEnumerator StaggerChunkRequests()
	{
		_chunksToLoad = new List<ChunkGameData>((BIOME_SIDE_LENGTH / CHUNK_SIZE) * (BIOME_SIDE_LENGTH / CHUNK_SIZE));
	
		foreach (Vector2Int chunkPos in GetChunkPositions())
		{
			_chunkNetworkManager.RequestChunkDataServerRpc(Player.LocalClientInstance.OwnerClientId, Player.LocalClientInstance.CurrentPlayerBiome.Value, chunkPos);
			yield return null;
		}

		Debug.Log($"ChunkManager: OnBiomeDataLoaded for {Player.LocalClientInstance.CurrentPlayerBiome.Value}");
	}
	
	public void LoadChunk(ChunkGameData chunkGameDataToLoad)
	{
		_chunksToLoad.Add(chunkGameDataToLoad);
		
		if(_chunksToLoad.Count == (BIOME_SIDE_LENGTH / CHUNK_SIZE) * (BIOME_SIDE_LENGTH / CHUNK_SIZE))
		{
			foreach (ChunkGameData chunk in _chunksToLoad)
			{
				OnLoadChunk?.Invoke(this, new ChunkEventArgs
				{
					Chunk = chunk
				});
			}
			
			TileManager.Instance.ExecuteTopTilePassthrough();
		}
	}

	public void UnloadAllPlayerChunks()
	{
		foreach (var item in GetChunksFromBiome(Player.LocalClientInstance.CurrentPlayerBiome.Value))
		{
			OnUnloadChunk?.Invoke(this, new ChunkEventArgs
			{
				Chunk = item.Value
			});
		}
	}
	
	public ChunkGameData GetChunkFromChunkPosition(BiomeType biome, Vector2Int chunkPosition)
	{
		switch(biome)
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
	
	public bool SetDoorState(Vector2Int doorPos, BiomeType biome, bool isOpen)
	{
		if(!IsServer) return false;
		
		ChunkGameData chunk = GetChunkFromAnyWorldPos(doorPos, biome);
		
		foreach (WorldObjectGameData worldObject in chunk.WorldObjectGameDataList)
		{
			if(worldObject.Position == doorPos)
			{
				// Found door
				var doorObject = worldObject as DoorObjectGameData;
				doorObject.SetDoorState(isOpen);
				
				return doorObject.IsOpen;
			}
		}
		
		return false;
	}
	
	public void DeserializeObjectDataToChunk(WorldObjectFileData worldObjectFileData, BiomeType biome, WorldObject worldObject, CardinalDirection orientation)
	{
		if(!IsServer) return;
		
		ChunkGameData chunk = GetChunkFromAnyWorldPos(worldObjectFileData.Pos, biome);
		
		chunk.DeserializeObjectData(worldObjectFileData, worldObject, orientation);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void AddObjectDataToChunkServerRpc(Vector2Int position, int worldObjectId, BiomeType biomeToPlaceIn, CardinalDirection orientation)
	{
		ChunkGameData chunk = GetChunkFromAnyWorldPos(position, biomeToPlaceIn);
		
		WorldObject worldObject = GameManager.Instance.GetWorldObjectFromID(worldObjectId);
		chunk.AddObjectData(position, worldObject, orientation);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void RemoveObjectDataFromChunkServerRpc(Vector2Int position, BiomeType biomeToRemoveFrom)
	{
		GetChunkFromAnyWorldPos(position, biomeToRemoveFrom).RemoveObjectData(position);
		TryToRemoveObjectClientRpc(position, biomeToRemoveFrom);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void TryToRemoveObjectClientRpc(Vector2Int position, BiomeType biomeToRemoveObjData)
	{
		if(Player.LocalClientInstance.CurrentPlayerBiome.Value != biomeToRemoveObjData) return;
		
		if(ObjectManager.Instance.TryToFindWorldObject(position, out WorldObject wo))
		{
			wo.DestroySelf();
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void PlaceTileServerRpc(Vector2Int position, int tileID, BiomeType biomeToAddTileData, TileType tileType)
	{
		ChunkGameData chunk = GetChunkFromAnyWorldPos(position, biomeToAddTileData);
		chunk.AddTileData(position, GameManager.Instance.GetTileSOFromID(tileID));
		
		if(tileType == TileType.Wall)	
		{
			Pathfinding.Instance.AddPfWallTileServerRpc(position, biomeToAddTileData);
		}
		
		HandleTileVisualClientRpc((Vector3Int)position, tileID, tileType, biomeToAddTileData);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void RemoveTileServerRpc(TileType tileType, Vector2Int position, BiomeType biome)
	{
		GetChunkFromAnyWorldPos(position, biome).RemoveTileDataIfExists(position, tileType);

		if (tileType == TileType.Wall || tileType == TileType.Ore)
		{
			Pathfinding.Instance.RemovePfWallTileServerRpc(position, biome);
		}

		HandleTileVisualClientRpc((Vector3Int)position, -1, tileType, biome);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void HandleTileVisualClientRpc(Vector3Int pos, int syncTileId, TileType syncTileType, BiomeType biome)
	{
		if(Player.LocalClientInstance.CurrentPlayerBiome.Value != biome) return;
		
		TileSO tileToPlace = null;
		
		if(syncTileId >= 0)
		{
			tileToPlace = GameManager.Instance.GetTileSOFromID(syncTileId);
		}

		// Chunk is loaded visually, therefore visually update whatever tile wants to be updated
		switch(syncTileType)
		{
			case TileType.Ground:
				TileManager.Instance.SetLocalTile(pos, tileToPlace == null ? null : tileToPlace, syncTileType);
				break;
			case TileType.Floor:
				TileManager.Instance.SetLocalTile(pos, tileToPlace == null ? null : tileToPlace, syncTileType);
				break;
			case TileType.Wall:
				TileManager.Instance.SetLocalTile(pos, tileToPlace == null ? null : tileToPlace, syncTileType);
				TileManager.Instance.AddTileVisibilityData(pos, new TileVisibility {Visibility = tileToPlace == null ? 0 : 1 });
				Lightmap.Instance.UpdateLightMap();
				TileManager.Instance.HandleTopWallTiles(pos, tileToPlace, TileManager.Instance.WallTm);
				break;
			case TileType.Ore:
				TileManager.Instance.SetLocalTile(pos, tileToPlace == null ? null : tileToPlace, syncTileType);
				TileManager.Instance.AddTileVisibilityData(pos, new TileVisibility { Visibility = tileToPlace == null ? 0 : 1 });
				Lightmap.Instance.UpdateLightMap();
				break;
		}
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

	public Dictionary<Vector2Int, ChunkGameData> GetChunksFromBiome(BiomeType biome)
	{
		switch(biome)
		{
			case BiomeType.Forest:
				return _forestChunks;
			case BiomeType.Cave:
				return _caveChunks;
		}
		
		Debug.LogError($"Biome {biome} should exist but doesn't, add environment chunks to ChunkManager");
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
		
		Debug.LogError("No Biome found for _activeEnvironment variable");
	}
}
