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
	
	public Vector2Int MinLoadedTilePosition { get; private set; }
	public Vector2Int MaxLoadedTilePosition { get; private set; }
	
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
	private Vector2 _loadPlayerPos, _unloadPlayerPos;
	private float _loadIncrementDist, _unloadIncrementDist;
	
	
	private void Awake()
	{
		Instance = this;
		_chunkNetworkManager = GetComponent<ChunkNetworkManager>();
	}
	
	private void Update()
	{
		if(Player.LocalClientInstance == null || IS_GENERATING_ENVIRONMENT || SaveSystem.Instance.IsDeserializing) return;
		
		Vector2Int newChunkPosition = GetChunkPosition(Player.LocalClientInstance.transform.position);
		if (newChunkPosition != _lastChunkPosition)
		{
			_lastChunkPosition = newChunkPosition;
			_currentChunkPosition = newChunkPosition;
			UpdateChunksAroundPlayer();
		}
		
		if(Vector2.Distance(Player.LocalClientInstance.transform.position, _loadPlayerPos) > _loadIncrementDist)
		{
			if(_chunksToLoad.Count > 0)
			{
				LoadChunk(_chunksToLoad.Dequeue());
			}
			
			_loadPlayerPos = Player.LocalClientInstance.transform.position;
		}
		
		if(Vector2.Distance(Player.LocalClientInstance.transform.position, _unloadPlayerPos) > _unloadIncrementDist)
		{
			if(_chunksToUnload.Count > 0)
			{
				UnloadChunk(_chunksToUnload.Dequeue());
			}
			
			_unloadPlayerPos = Player.LocalClientInstance.transform.position;
		}
	}

	private Vector2Int GetChunkPosition(Vector3 worldPosition)
	{
		int chunkSize = CHUNK_SIZE;
		return new Vector2Int(
			Mathf.FloorToInt(worldPosition.x / chunkSize),
			Mathf.FloorToInt(worldPosition.y / chunkSize)
		);
	}
	
	public void UpdateChunksAroundPlayer()
	{
		if(WorldManager.Instance.GetIsTransitioningEnvironment()) return;
	
		if(Player.LocalClientInstance.IsHost)
		{
			HostUpdatePlayerChunks();
		}
		else
		{
			_chunkNetworkManager.ClientUpdatePlayerChunks(Player.LocalClientInstance.PlayerEnvironment.Value);
		}
	}
	
	private void HostUpdatePlayerChunks()
	{
		// Debug.Log("Updating Chunks as Host");
	
		// Get chunks around the player the player wants to load
		List<Vector2Int> chunksToLoadAroundPlayer = GetPositionsToLoadAroundPlayer();
		
		// For each of those chunks, load them if they are not already loaded
		foreach (Vector2Int chunkPos in chunksToLoadAroundPlayer)
		{
			if(!_loadedChunks.ContainsKey(chunkPos))
			{
				_chunksToLoad.Enqueue(chunkPos);
			}
		}
		
		_loadIncrementDist = (float)CHUNK_SIZE / _chunksToLoad.Count;
		
		// In the loaded player chunks, if any of them are not in playerChunksToLoadAroundPlayer, unload them
		foreach (Vector2Int chunkPos in _loadedChunks.Keys.ToList())
		{
			if (!chunksToLoadAroundPlayer.Contains(chunkPos))
			{
				_chunksToUnload.Enqueue(chunkPos);
				
			}
		}
		
		_unloadIncrementDist = (float)CHUNK_SIZE / _chunksToUnload.Count;
		// InvokeOnLoadedPlayerChunksUpdated();
	}

	private void LoadChunk(Vector2Int chunkPos)
	{
		ChunkGameData chunkToLoad = GetChunkDataFromChunkPosition(Player.LocalClientInstance.PlayerEnvironment.Value, chunkPos);
		InvokeOnLoadChunk(chunkToLoad);
	}

	private void UnloadChunk(Vector2Int chunkPos)
	{
		ChunkGameData chunkToUnload = GetChunkDataFromChunkPosition(Player.LocalClientInstance.PlayerEnvironment.Value, chunkPos);
		InvokeOnUnloadChunk(chunkToUnload);
	}

	public void InvokeOnLoadedPlayerChunksUpdated()
	{
		CalculateMinMaxLoadedTilePos();
		
		OnLoadedPlayerChunksUpdated?.Invoke(this, new OnActiveChunksUpdatedEventArgs
		{
			MinLoadedTilePos = MinLoadedTilePosition,
			MaxLoadedTilePos = MaxLoadedTilePosition
		});
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
	
	public void InvokeOnLoadChunk(ChunkGameData chunkGameDataToLoad)
	{
		// Add chunk to _loadedChunks
		if(chunkGameDataToLoad == null)
		{
			Debug.LogWarning("chunk to load is null");
			return;
		}
		
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
	
	public void ClearChunkVisuals()
	{
		for (int i = _loadedChunks.Count - 1; i >= 0; i--)
		{
			var chunk = _loadedChunks.ElementAt(i);
			InvokeOnUnloadChunk(chunk.Value);
		}
	}
	
	public ChunkGameData GetChunkDataFromChunkPosition(EnvironmentID environment, Vector2Int chunkPosition)
	{
		switch(environment)
		{
			case EnvironmentID.Forest:
				return _forestChunks.ContainsKey(chunkPosition) ? _forestChunks[chunkPosition] : null;
			case EnvironmentID.Cave:
				return _caveChunks.ContainsKey(chunkPosition) ? _caveChunks[chunkPosition] : null;
		}
		
		Debug.LogError("No Environment found for _activeEnvironment variable");
		return null;
	}
	
	public void AddObjectDataToChunk(Vector2Int position, WorldObject worldObject, EnvironmentID environmentToPlaceIn)
	{
		if(!IsServer) return;
		
		ChunkGameData chunk = GetChunk(position, environmentToPlaceIn);
		
		chunk.AddObjectData(position, worldObject);
	}
	
	public void RemoveObjectDataFromChunk(Vector2Int position, EnvironmentID environmentToRemoveFrom)
	{
		if(!IsServer) return;
		
		ChunkGameData chunk = GetChunk(position, environmentToRemoveFrom);
		
		chunk.RemoveObjectData(position);
	}
	
	public void AddWallTileDataToChunk(Vector2Int position, byte tileID, EnvironmentID environmentToAddTileData)
	{
		if(!IsServer) return;
	
		// Get chunk tile was placed in
		ChunkGameData chunk = GetChunk(position, environmentToAddTileData);
		
		// Add that tile data to this chunk
		chunk.AddWallTileData(position, GameManager.Instance.GetTileSOFromID(tileID));
	}
	
	public void RemoveWallTileDataFromChunk(Vector2Int position, EnvironmentID environmentToRemoveTileData)
	{
		if(!IsServer) return;
	
		// Get chunk tile was destroyed in
		ChunkGameData chunk = GetChunk(position, environmentToRemoveTileData);
		
		// Delete that tile data from this chunk
		chunk.RemoveWallTileData(position);
	}
	
	public ChunkGameData GetChunk(Vector2Int position, EnvironmentID environmentToGetChunkFrom)
	{
		Vector2Int chunkCoord = GetChunkCoordFromPosition(position);
		
		var chunks = GetChunksFromEnvironment(environmentToGetChunkFrom);
		chunks.TryGetValue(chunkCoord, out ChunkGameData chunk);
		
		return chunk;
	}
	
	private Vector2Int GetChunkCoordFromPosition(Vector2 position)
	{
		int chunkX = Mathf.FloorToInt(position.x / CHUNK_SIZE);
		int chunkY = Mathf.FloorToInt(position.y / CHUNK_SIZE);
		return new Vector2Int(chunkX, chunkY);
	}

	public Dictionary<Vector2Int, ChunkGameData> GetChunksFromEnvironment(EnvironmentID environmentToGet)
	{
		switch(environmentToGet)
		{
			case EnvironmentID.Forest:
				return _forestChunks;
			case EnvironmentID.Cave:
				return _caveChunks;
		}
		
		Debug.LogError($"Environment {environmentToGet} should exist but doesn't, add environment chunks to ChunkManager");
		return null;
	}
	

	public void SetChunksForEnvironment(EnvironmentID environmentToSetChunksFor, Dictionary<Vector2Int, ChunkGameData> newChunks)
	{
		switch(environmentToSetChunksFor)
		{
			case EnvironmentID.Forest:
				_forestChunks = newChunks;
				return;
			case EnvironmentID.Cave:
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
	}
	
	public Dictionary<Vector2Int, ChunkGameData> GetLoadedPlayerChunks()
	{
		return _loadedChunks;
	}
}
