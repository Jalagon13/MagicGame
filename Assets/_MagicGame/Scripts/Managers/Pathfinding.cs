using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : NetworkBehaviour
{
	public static Pathfinding Instance { get; private set; }
	
	[SerializeField] private TileBase _walkableTile;
	
	private Dictionary<EnvironmentID, HashSet<Vector2Int>> _environmentToLoadedPathfindingChunks = new();
	private Dictionary<ulong, HashSet<Vector2Int>> _playerToChunks = new(); // Keeps track of chunks loaded by player using clientId
	
	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		GameInput.Instance.OnResearchMenuButton += GameInput_OnResearchMenuButton;
	}

    private void GameInput_OnResearchMenuButton(object sender, EventArgs e)
    {
		IsPositionOnWalkableTile(ActionManager.MouseWorldPosition, Player.LocalClientInstance.PlayerEnvironment.Value);
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
			
			var environment = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().PlayerEnvironment.Value;
			
			// Loop through all chunks this player has loaded and try to remove them from the pathfinding tilemap
			foreach (Vector2Int chunkPos in _playerToChunks[clientId])
			{
				if(!IsChunkInUse(chunkPos, environment))
				{
					_environmentToLoadedPathfindingChunks[environment].Remove(chunkPos);
					
					if(_environmentToLoadedPathfindingChunks[environment].Count <= 0)
					{
						_environmentToLoadedPathfindingChunks.Remove(environment);
					}
				}
			}
		}
	}
	
	public bool IsPositionOnWalkableTile(Vector2 position, EnvironmentID environment)
	{
		if(!IsServer)
		{
			Debug.LogWarning($"Should not be calling this on a client. Only server. Returning false");
			return false;
		}
	
		Vector2Int tilePositionToCheck = Vector2Int.FloorToInt(position);
		
		// Calculate chunk coordinates
		int chunkX = Mathf.FloorToInt(position.x / ChunkManager.CHUNK_SIZE);
		int chunkY = Mathf.FloorToInt(position.y / ChunkManager.CHUNK_SIZE);
		var chunkPos = new Vector2Int(chunkX, chunkY);
		
		if(_environmentToLoadedPathfindingChunks[environment].Contains(chunkPos))
		{
			// Loop through all the tile positions of this chunk
			for (int x = 0; x < ChunkManager.CHUNK_SIZE; x++)
			{
				for (int y = 0; y < ChunkManager.CHUNK_SIZE; y++)
				{
					// Find the tilePosition and return whether it is walkable or not
					int tilePosX = chunkPos.x * ChunkManager.CHUNK_SIZE + x;
					int tilePosY = chunkPos.y * ChunkManager.CHUNK_SIZE + y;
					Vector2Int chunkTileWorldPosition = new(tilePosX, tilePosY);
					
					if(chunkTileWorldPosition == tilePositionToCheck)
					{
						// Found it, check if it is walkable
						bool isWalkable = CheckTileWalkable(tilePositionToCheck, ChunkManager.Instance.GetChunkData(environment, chunkPos).WallTileGameDataList);
						
						if(isWalkable)
						{
							Debug.LogWarning($"This position is on a walkable tile");
						}
						else
						{
							Debug.LogWarning($"This position is NOT on a walkable tile. Wall tile");
						}
						
						return isWalkable;
					}
				}
			}
		}
		else
		{
			Debug.LogWarning($"Position is not part of a loaded chunk");
			return false;
		}
		
		Debug.LogWarning($"Found chunk pos but position is not in this chunk. This message should never appear");
		return false;
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

	public void UpdateChunkPathfinding(Vector2Int chunkPos, ChunkGameData chunkGameData, EnvironmentID environment, ulong clientId)
	{
		// No matter what, add this chunk to this player's chunk list
		_playerToChunks[clientId].Add(chunkPos);

		if(_environmentToLoadedPathfindingChunks.ContainsKey(environment))
		{
			if(!_environmentToLoadedPathfindingChunks[environment].Contains(chunkPos))
			{
				_environmentToLoadedPathfindingChunks[environment].Add(chunkGameData.ChunkPosition);
			}
		}
		else
		{
			_environmentToLoadedPathfindingChunks.Add(environment, new());
			_environmentToLoadedPathfindingChunks[environment].Add(chunkGameData.ChunkPosition);
		}
	}

	public void RequestUnloadChunk(Vector2Int chunkPos, ulong clientId, EnvironmentID environment)
	{
		RequestUnloadChunkServerRpc(chunkPos, clientId, environment);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RequestUnloadChunkServerRpc(Vector2Int chunkPos, ulong clientId, EnvironmentID environment)
	{
		_playerToChunks[clientId].Remove(chunkPos);
	
		if(!IsChunkInUse(chunkPos, environment))
		{
			_environmentToLoadedPathfindingChunks[environment].Remove(chunkPos);
			
			if(_environmentToLoadedPathfindingChunks[environment].Count <= 0)
			{
				_environmentToLoadedPathfindingChunks.Remove(environment);
			}
		}
	}

	private bool IsChunkInUse(Vector2Int chunkPos, EnvironmentID environment)
	{
		// If a player still has chunk active
		foreach (var kvp in _playerToChunks)
		{
			// Loop through only players in the same environment being tested
			if(NetworkManager.ConnectedClients[kvp.Key].PlayerObject.GetComponent<Player>().PlayerEnvironment.Value != environment) continue;
		
			var chunksLoaded = kvp.Value;
			
			if(chunksLoaded.Contains(chunkPos))
			{
				// This chunk is still in use by this player, return true
				return true;
			}
		}
		
		return false;
	}
}
