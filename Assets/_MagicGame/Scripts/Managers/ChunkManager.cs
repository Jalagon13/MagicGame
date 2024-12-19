using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class ChunkManager : NetworkBehaviour
{
	public static bool IS_GENERATING_ENVIRONMENT;
	public static int ENVIRONMENT_SIDE_LENGTH = 256;
	public static int CHUNK_SIZE = 4;

	public event EventHandler<OnActiveChunksUpdatedEventArgs> OnLoadedPlayerChunksUpdated; // Whenever new chunks are loaded around the player
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
	
	[SerializeField] private int _chunkLoadRadiusX = 5;
	[SerializeField] private int _chunkLoadRadiusY = 4;
	
	private Dictionary<Vector2Int, ChunkGameData> _loadedPlayerChunks = new();
	private Dictionary<Vector2Int, ChunkGameData> _forestChunks = new();
	private Dictionary<Vector2Int, ChunkGameData> _caveChunks = new();
	private Vector2 _lastPlayerPosition;
	private Vector2Int _playerChunkCoord;
	private Vector2Int _minLoadedTilePos;
	private Vector2Int _maxLoadedTilePos;
	private ChunkNetworkManager _chunkNetworkManager;
	
	private void Awake()
	{
		Instance = this;
		_chunkNetworkManager = GetComponent<ChunkNetworkManager>();
	}
	
	private void Update()
	{
		if(Player.LocalClientInstance == null || IS_GENERATING_ENVIRONMENT) return;
		
		Vector3Int playerTilePos = GetPlayerTilePos();
		
		// NTFS: Optimize this in the future so it doesn't unload all the chunks and reload them
		if (Vector2.Distance(Player.LocalClientInstance.transform.position, _lastPlayerPosition) >= CHUNK_SIZE) 
		{
			_playerChunkCoord = new Vector2Int(playerTilePos.x / CHUNK_SIZE, playerTilePos.y / CHUNK_SIZE);
			
			// Hosts use the single player logic, clients use netcode logic
			UpdateChunksAroundPlayer();
			
			// Update last player position
			_lastPlayerPosition = Player.LocalClientInstance.transform.position;
		}
	}
	
	public void UpdateChunksAroundPlayer()
	{
		if(Player.LocalClientInstance.IsHost)
		{
			SinglePlayerUpdatePlayerChunks();
		}
		else
		{
			_chunkNetworkManager.MultiplayerUpdatePlayerChunks();
		}
	}
	
	private void SinglePlayerUpdatePlayerChunks()
	{
		// Debug.Log("Updating Chunks as Host");
	
		// Get chunks around the player the player wants to load
		List<Vector2Int> playerChunksToLoadAroundPlayer = GetChunkPositionsToLoadAroundPlayer();
		List<Vector2Int> loadedChunkPositions = new(_loadedPlayerChunks.Keys);
		
		// For each of those chunks, load them if they are not already loaded
		foreach (Vector2Int chunkPosition in playerChunksToLoadAroundPlayer)
		{
			TryToLoadChunk(chunkPosition);
		}
		
		// In the loaded player chunks, if any of them are not in playerChunksToLoadAroundPlayer, unload them
		foreach (Vector2Int loadedChunkPosition in loadedChunkPositions)
		{
			if (!playerChunksToLoadAroundPlayer.Contains(loadedChunkPosition))
			{
				TryToUnloadChunk(loadedChunkPosition);
			}
		}
	
		InvokeOnLoadedPlayerChunksUpdated();
	}
	
	public void InvokeOnLoadedPlayerChunksUpdated()
	{
		CalculateMinMaxLoadedTilePos();
		
		OnLoadedPlayerChunksUpdated?.Invoke(this, new OnActiveChunksUpdatedEventArgs
		{
			MinLoadedTilePos = _minLoadedTilePos,
			MaxLoadedTilePos = _maxLoadedTilePos
		});
	}
	
	public void UnloadAllPlayerChunks()
	{
		for (int i = _loadedPlayerChunks.Count - 1; i >= 0; i--)
		{
			var chunk = _loadedPlayerChunks.ElementAt(i);
			TryToUnloadChunk(chunk.Key);
		}
	}
	
	private void TryToUnloadChunk(Vector2Int chunkPos)
	{
		ChunkGameData chunkToUnload = GetChunkDataFromChunkPosition(chunkPos);
		InvokeOnUnloadChunk(chunkToUnload);
	}
	
	private void TryToLoadChunk(Vector2Int chunkPos)
	{
		ChunkGameData chunkToLoad = GetChunkDataFromChunkPosition(chunkPos);
		InvokeOnLoadChunk(chunkToLoad);
	}
	
	public void InvokeOnUnloadChunk(ChunkGameData chunkGameDataToUnload)
	{
		// Remove the chunk from the list of loaded chunks
		if(_loadedPlayerChunks.ContainsKey(chunkGameDataToUnload.ChunkPosition))
		{
			OnUnloadChunk?.Invoke(this, new ChunkEventArgs
			{
				Chunk = chunkGameDataToUnload
			});
		
			_loadedPlayerChunks.Remove(chunkGameDataToUnload.ChunkPosition);
		}
	}
	
	public void InvokeOnLoadChunk(ChunkGameData chunkGameDataToLoad)
	{
		// Add chunk to _loadedChunks
		if (!_loadedPlayerChunks.ContainsKey(chunkGameDataToLoad.ChunkPosition))
		{
			OnLoadChunk?.Invoke(this, new ChunkEventArgs
			{
				Chunk = chunkGameDataToLoad
			});
		
			_loadedPlayerChunks.Add(chunkGameDataToLoad.ChunkPosition, chunkGameDataToLoad);
		}
	}
	
	public ChunkGameData GetChunkDataFromChunkPosition(Vector2Int chunkPosition)
	{
		switch(WorldManager.Instance.GetActiveEnvironmentID())
		{
			case WorldManager.EnvironmentID.Forest:
				return _forestChunks.ContainsKey(chunkPosition) ? _forestChunks[chunkPosition] : null;
			case WorldManager.EnvironmentID.Cave:
				return _caveChunks.ContainsKey(chunkPosition) ? _caveChunks[chunkPosition] : null;
		}
		
		Debug.LogError("No Environment found for _activeEnvironment variable");
		return null;
	}
	
	public void AddWorldAssetDataToChunk(Vector2Int position, WorldObject worldObject)
	{
		if(!IsServer) return;
		
		ChunkGameData chunk = GetChunk(position);
		chunk.AddWorldAssetData(position, worldObject);
	}
	
	public void RemoveWorldAssetDataFromChunk(Vector2Int position)
	{
		if(!IsServer) return;
		
		ChunkGameData chunk = GetChunk(position);
		
		chunk.RemoveWorldAssetData(position);
	}
	
	public void AddWallTileDataToChunk(Vector2Int position, byte tileID)
	{
		if(!IsServer) return;
	
		// Get chunk tile was placed in
		ChunkGameData chunk = GetChunk(position);
		
		// Add that tile data to this chunk
		chunk.AddWallTileData(position, GameManager.Instance.GetTileSOFromID(tileID));
	}
	
	public void RemoveWallTileDataFromChunk(Vector2Int position)
	{
		if(!IsServer) return;
	
		// Get chunk tile was destroyed in
		ChunkGameData chunk = GetChunk(position);
		
		// Delete that tile data from this chunk
		chunk.RemoveWallTileData(position);
	}
	
	public ChunkGameData GetChunk(Vector2Int position)
	{
		Vector2Int chunkCoord = GetChunkCoordFromPosition(position);
		
		var activeEnvironmentChunks = GetChunksFromActiveEnvironment();
		activeEnvironmentChunks.TryGetValue(chunkCoord, out ChunkGameData chunk);
		
		return chunk;
	}
	
	public List<Vector2Int> GetChunkPositionsToLoadAroundPlayer()
	{
		List<Vector2Int> chunksToLoad = new();
		
		for (int x = -_chunkLoadRadiusX; x <= _chunkLoadRadiusX; x++)
		{
			for (int y = -_chunkLoadRadiusY; y <= _chunkLoadRadiusY; y++)
			{
				Vector2Int chunkCoord = new Vector2Int(_playerChunkCoord.x + x, _playerChunkCoord.y + y);
				chunksToLoad.Add(chunkCoord);
			}
		}
		
		return chunksToLoad;
	}
	
	private Vector2Int GetChunkCoordFromPosition(Vector2 position)
	{
		int chunkX = Mathf.FloorToInt(position.x / CHUNK_SIZE);
		int chunkY = Mathf.FloorToInt(position.y / CHUNK_SIZE);
		return new Vector2Int(chunkX, chunkY);
	}
	
	public Dictionary<Vector2Int, ChunkGameData> GetChunksFromActiveEnvironment()
	{
		switch(WorldManager.Instance.GetActiveEnvironmentID())
		{
			case WorldManager.EnvironmentID.Forest:
				return _forestChunks;
			case WorldManager.EnvironmentID.Cave:
				return _caveChunks;
		}
		
		Debug.LogError("No Environment found for _activeEnvironment variable");
		return null;
	}
	
	public void SetChunksFromActiveEnvironment(Dictionary<Vector2Int, ChunkGameData> newChunks)
	{
		switch(WorldManager.Instance.GetActiveEnvironmentID())
		{
			case WorldManager.EnvironmentID.Forest:
				_forestChunks = newChunks;
				return;
			case WorldManager.EnvironmentID.Cave:
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
	

	private void CalculateMinMaxLoadedTilePos()
	{
		// Set min and max loaded tile positions by looping through loaded chunks
		Vector2Int minLoadedTilePos = new(int.MaxValue, int.MaxValue);
		Vector2Int maxLoadedTilePos = new(int.MinValue, int.MinValue);

		foreach (var item in _loadedPlayerChunks)
		{
			Vector2Int loadedChunkWorldPosition = item.Key * CHUNK_SIZE;
			minLoadedTilePos = Vector2Int.Min(minLoadedTilePos, loadedChunkWorldPosition);
			maxLoadedTilePos = Vector2Int.Max(maxLoadedTilePos, loadedChunkWorldPosition);
		}

		// Add chunk size to maxLoadedTilePosCoord to account for the chunk's area
		maxLoadedTilePos += new Vector2Int(CHUNK_SIZE, CHUNK_SIZE);

		// Set the final values
		_minLoadedTilePos = minLoadedTilePos;
		_maxLoadedTilePos = maxLoadedTilePos;
	}
	
	public Dictionary<Vector2Int, ChunkGameData> GetForestChunks()
	{
		return _forestChunks;
	}
	
	public Dictionary<Vector2Int, ChunkGameData> GetCaveChunks()
	{
		return _caveChunks;
	}
	
	public Dictionary<Vector2Int, ChunkGameData> GetLoadedPlayerChunks()
	{
		return _loadedPlayerChunks;
	}
}
