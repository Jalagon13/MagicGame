using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PathfindingData
{
	public HashSet<Vector2Int> Chunks { get; }
	public Tilemap WallColliderTm { get; }

	public PathfindingData(HashSet<Vector2Int> chunks, Tilemap tilemap)
	{
		Chunks = chunks;
		WallColliderTm = tilemap;
	}
}

public class Pathfinding : NetworkBehaviour
{
	public static Pathfinding Instance { get; private set; }
	public event EventHandler<PathfindingTilemapEventArgs> OnPathfindingTilemapCreated;
	public class PathfindingTilemapEventArgs : EventArgs
	{
		public Collider2D TilemapCollider;
		public BiomeType Biome;
	}
	
	[SerializeField] private TileBase _wallTile;
	[SerializeField] private Tilemap _wallColliderTmPrefab;
	
	public Dictionary<BiomeType, PathfindingData> BiomeToLoadedPathfindingChunks { get; private set; } = new();
	private Dictionary<ulong, HashSet<Vector2Int>> _playerToChunks = new(); // Keeps track of chunks loaded by player using clientId
	private BiomeType _currentPlayerBiome;
	
	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		WorldManager.Instance.OnBiomeDataLoaded += CacheCurrentPlayerBiome;
	}

	public TilemapCollider2D GetPathfindingWallCollider(BiomeType biome)
	{
		if(BiomeToLoadedPathfindingChunks.ContainsKey(biome))
		{
			return BiomeToLoadedPathfindingChunks[biome].WallColliderTm.GetComponent<TilemapCollider2D>();
		}
		else
		{
		    return null;
		}
	}

	private void CacheCurrentPlayerBiome(object sender, EventArgs e)
	{
		_currentPlayerBiome = Player.LocalClientInstance.CurrentPlayerBiome.Value;
	}

	public void OnClientConnected(ulong clientId)
	{
		if(!_playerToChunks.ContainsKey(clientId))
		{
			_playerToChunks.Add(clientId, new());
		}
	}

	public void OnClientDisconnected(ulong clientId)
	{
		if(_playerToChunks.ContainsKey(clientId))
		{
			// Loop through all chunks this player has loaded and try to remove them from the pathfinding tilemap
			foreach (Vector2Int chunkPos in _playerToChunks[clientId])
			{
				if(!IsChunkInUse(chunkPos, _currentPlayerBiome, clientId))
				{
					BiomeToLoadedPathfindingChunks[_currentPlayerBiome].Chunks.Remove(chunkPos);
					
					RemoveWallColliderChunk(_currentPlayerBiome, chunkPos);
					
					if(BiomeToLoadedPathfindingChunks[_currentPlayerBiome].Chunks.Count <= 0)
					{
						Destroy(BiomeToLoadedPathfindingChunks[_currentPlayerBiome].WallColliderTm.gameObject);
						BiomeToLoadedPathfindingChunks.Remove(_currentPlayerBiome);
					}
				}
			}
			
			_playerToChunks.Remove(clientId);
		}
	}
	
	public Dictionary<BiomeType, TilemapCollider2D> GetExistingPathfindingBiomes()
	{
		Dictionary<BiomeType, TilemapCollider2D> biomeTmColliderPair = new();
	
		if(BiomeToLoadedPathfindingChunks.Count > 0)
		{
			foreach (var kvp in BiomeToLoadedPathfindingChunks)
			{
				biomeTmColliderPair.Add(kvp.Key, kvp.Value.WallColliderTm.GetComponent<TilemapCollider2D>());
			}
		}
		
		return biomeTmColliderPair;
	}
	
	public bool EnvironmentPathfindingExists(BiomeType environment)
	{
		return BiomeToLoadedPathfindingChunks.ContainsKey(environment);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void AddPfWallTileServerRpc(Vector2Int position, BiomeType biome)
	{
		if(!BiomeToLoadedPathfindingChunks.ContainsKey(biome))
		{
		    Debug.LogWarning($"Biome {biome} does not have a pathfinding tilemap loaded. Can't add wall tile");
		    return;
		}
		
		if(!BiomeToLoadedPathfindingChunks[biome].WallColliderTm.HasTile((Vector3Int)position))
		{
			BiomeToLoadedPathfindingChunks[biome].WallColliderTm.SetTile((Vector3Int)position, _wallTile);
		}
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void RemovePfWallTileServerRpc(Vector2Int position, BiomeType biome)
	{
		if(!BiomeToLoadedPathfindingChunks.ContainsKey(biome)) return;
		
		BiomeToLoadedPathfindingChunks[biome].WallColliderTm.SetTile((Vector3Int)position, null);
	}

	public void UpdateChunkPathfinding(Vector2Int chunkPos, ChunkGameData chunkGameData, BiomeType biome, ulong clientId)
	{
		// No matter what, add this chunk to this player's chunk list
		_playerToChunks[clientId].Add(chunkPos);

		if(BiomeToLoadedPathfindingChunks.ContainsKey(biome))
		{
			if(!BiomeToLoadedPathfindingChunks[biome].Chunks.Contains(chunkPos))
			{
				AddWallColliderChunk(biome, chunkGameData);
			}
		}
		else
		{
			BiomeToLoadedPathfindingChunks.Add(biome, new PathfindingData(new(), CreateWallColliderTilemap(biome)));
			AddWallColliderChunk(biome, chunkGameData);
		}
	}
	
	private void AddWallColliderChunk(BiomeType biome, ChunkGameData chunkGameData)
	{
		BiomeToLoadedPathfindingChunks[biome].Chunks.Add(chunkGameData.ChunkPosition);
		
		// Loop through all the wall data and inst a wall tile for it on the tilemap
		foreach (TileGameData wallTileGameData in chunkGameData.WallTileGameDataList)
		{
			BiomeToLoadedPathfindingChunks[biome].WallColliderTm.SetTile((Vector3Int)wallTileGameData.TilePosition, _wallTile);
		}
	}

	private Tilemap CreateWallColliderTilemap(BiomeType biome)
	{
		var wallColliderTm = Instantiate(_wallColliderTmPrefab);
		wallColliderTm.GetComponent<PathfindingWallTm>().SetBiome(biome);
		wallColliderTm.transform.SetParent(transform);
		wallColliderTm.gameObject.name = $"{biome}{_wallColliderTmPrefab.name}";
		
		OnPathfindingTilemapCreated?.Invoke(this, new PathfindingTilemapEventArgs
		{
			TilemapCollider = wallColliderTm.GetComponent<TilemapCollider2D>(),
			Biome = biome
		});
		
		return wallColliderTm;
	}

	public void RequestUnloadChunk(Vector2Int chunkPos, ulong clientId, BiomeType environment)
	{
		RequestUnloadChunkServerRpc(chunkPos, clientId, environment);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RequestUnloadChunkServerRpc(Vector2Int chunkPos, ulong clientId, BiomeType biome)
	{
		_playerToChunks[clientId].Remove(chunkPos);

		if(!IsChunkInUse(chunkPos, biome, clientId))
		{
			if(!BiomeToLoadedPathfindingChunks.ContainsKey(biome)) return;
		
			BiomeToLoadedPathfindingChunks[biome].Chunks.Remove(chunkPos);
			
			RemoveWallColliderChunk(biome, chunkPos);
			
			if(BiomeToLoadedPathfindingChunks[biome].Chunks.Count <= 0)
			{
				Destroy(BiomeToLoadedPathfindingChunks[biome].WallColliderTm.gameObject);
				BiomeToLoadedPathfindingChunks.Remove(biome);
			}
		}
	}

	private void RemoveWallColliderChunk(BiomeType environment, Vector2Int chunkPos)
	{
		Tilemap wallColliderTm = BiomeToLoadedPathfindingChunks[environment].WallColliderTm;
		
		// Loop through all positions inside this chunk
		for (int x = 0; x < ChunkManager.CHUNK_SIZE; x++)
		{
			for (int y = 0; y < ChunkManager.CHUNK_SIZE; y++)
			{
				// Get the world position of each tile in the chunk
				int tilePosX = chunkPos.x * ChunkManager.CHUNK_SIZE + x;
				int tilePosY = chunkPos.y * ChunkManager.CHUNK_SIZE + y;
				Vector3Int tileWorldPosition = new(tilePosX, tilePosY);
				
				if(wallColliderTm.HasTile(tileWorldPosition))
				{
					wallColliderTm.SetTile(tileWorldPosition, null);
				}
			}
		}
	}

	// Checks if another player needs the pathfinding for this chunk
	private bool IsChunkInUse(Vector2Int chunkPos, BiomeType biome, ulong clientId)
	{
		// If a player still has chunk active
		foreach (var kvp in _playerToChunks)
		{
			if(kvp.Key == clientId) continue; // Only process other players need the chunk in use

			// Loop through only players in the same environment being tested
			if(NetworkManager.ConnectedClients[kvp.Key].PlayerObject.GetComponent<Player>().CurrentPlayerBiome.Value != biome) continue;
		
			var chunksLoaded = kvp.Value;
			
			if(chunksLoaded.Contains(chunkPos))
			{
				// This chunk is still in use by this player, return true
				return true;
			}
		}
		
		return false;
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		
		WorldManager.Instance.OnBiomeDataLoaded -= CacheCurrentPlayerBiome;
	}
}
