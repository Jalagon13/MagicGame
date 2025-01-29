using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : NetworkBehaviour
{
	public static Pathfinding Instance { get; private set; }
	
	[SerializeField] private TileBase _walkableTile;
	
	private HashSet<Vector2Int> _loadedForestPathfindingChunks = new();
	private Dictionary<ulong, HashSet<Vector2Int>> _playerToChunks = new(); // Keeps track of chunks loaded by player using clientId
	private Tilemap _pathfindingTilemap;
	
	private void Awake()
	{
		Instance = this;
		_pathfindingTilemap = GetComponent<Tilemap>();
	}

	public void OnClientConnected(ulong clientId)
	{
		if(!_playerToChunks.ContainsKey(clientId))
		{
			Debug.Log($"Creating player {clientId}'s pathfinding chunks list");
			_playerToChunks.Add(clientId, new());
		}
	}

	public void OnClientDisconnected(ulong clientId)
	{
		if(_playerToChunks.ContainsKey(clientId))
		{
			Debug.Log($"Removing player {clientId}'s pathfinding chunks list");
			_playerToChunks.Remove(clientId);
			
			// Loop through all chunks this player has loaded and try to remove them from the pathfinding tilemap
			foreach (Vector2Int chunkPos in _playerToChunks[clientId])
			{
				if(!IsChunkInUse(chunkPos))
				{
					RemovePathfindingForChunk(chunkPos);
			
					_loadedForestPathfindingChunks.Remove(chunkPos);
				}
			}
		}
	}
	
	public bool IsPositionOnWalkableTile(Vector2 position)
	{
		Vector3Int tilePosition = Vector3Int.FloorToInt(position);
		// If a tile exists on this tilemap at this location it is walkable.
		return _pathfindingTilemap.HasTile(tilePosition);
	}

	public void UpdateChunkPathfinding(Vector2Int chunkPos, ChunkGameData chunkGameData, EnvironmentID environment, ulong clientId)
	{
		// No matter what, add this chunk to this player's chunk list
		_playerToChunks[clientId].Add(chunkPos);
	
		if(!_loadedForestPathfindingChunks.Contains(chunkPos))
		{
			PopulatePathfindingTilemap(chunkGameData);
			
			_loadedForestPathfindingChunks.Add(chunkGameData.ChunkPosition);
		}
	}

	private void PopulatePathfindingTilemap(ChunkGameData chunkGameData)
	{
		for (int x = 0; x < ChunkManager.CHUNK_SIZE; x++)
		{
			for (int y = 0; y < ChunkManager.CHUNK_SIZE; y++)
			{
				// Get the world position of each tile in the chunk
				int tilePosX = chunkGameData.ChunkPosition.x * ChunkManager.CHUNK_SIZE + x;
				int tilePosY = chunkGameData.ChunkPosition.y * ChunkManager.CHUNK_SIZE + y;
				Vector2Int tileWorldPosition = new(tilePosX, tilePosY);
				
				bool isWalkable = CheckTileWalkable(tileWorldPosition, chunkGameData.WallTileGameDataList);
				
				_pathfindingTilemap.SetTile((Vector3Int)tileWorldPosition, isWalkable ? _walkableTile : null);
			}
		}
	}

	private bool CheckTileWalkable(Vector2Int tileWorldPosition, List<TileGameData> wallTileGameDataList)
	{
		foreach (var wallTileGameData in wallTileGameDataList)
		{
			var wallTilePosition = wallTileGameData.TilePosition;
			
			if(wallTilePosition == tileWorldPosition)
			{
				return false;
			}
		}
	
		return true;
	}
	
	public void RequestUnloadChunk(Vector2Int chunkPos, ulong clientId)
	{
		RequestUnloadChunkServerRpc(chunkPos, clientId);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RequestUnloadChunkServerRpc(Vector2Int chunkPos, ulong clientId)
	{
		_playerToChunks[clientId].Remove(chunkPos);
	
		if(!IsChunkInUse(chunkPos))
		{
			RemovePathfindingForChunk(chunkPos);
			
			_loadedForestPathfindingChunks.Remove(chunkPos);
		}
	}

	private bool IsChunkInUse(Vector2Int chunkPos)
	{
		// If a player still has chunk active
		foreach (var kvp in _playerToChunks)
		{
			var chunksLoaded = kvp.Value;
			
			if(chunksLoaded.Contains(chunkPos))
			{
				// This chunk is still in use by this player, return true
				return true;
			}
		}
		
		return false;
	}

	private void RemovePathfindingForChunk(Vector2Int chunkPos)
	{
		for (int x = 0; x < ChunkManager.CHUNK_SIZE; x++)
		{
			for (int y = 0; y < ChunkManager.CHUNK_SIZE; y++)
			{
				// Get the world position of each tile in the chunk
				int tilePosX = chunkPos.x * ChunkManager.CHUNK_SIZE + x;
				int tilePosY = chunkPos.y * ChunkManager.CHUNK_SIZE + y;
				Vector2Int tileWorldPosition = new(tilePosX, tilePosY);
				
				_pathfindingTilemap.SetTile((Vector3Int)tileWorldPosition, null);
			}
		}
	}
}
