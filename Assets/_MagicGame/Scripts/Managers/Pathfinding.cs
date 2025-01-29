using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Pathfinding : NetworkBehaviour
{
	public static Pathfinding Instance { get; private set; }
	
	private Tilemap _pathfindingTilemap;
	
	private void Awake()
	{
		Instance = this;
		_pathfindingTilemap = GetComponent<Tilemap>();
	}
	
	public void AddPathfindingTiles(Vector2Int chunkPos, ChunkGameData chunkGameData, EnvironmentID environment)
	{
		Debug.Log($"Updating pathfinding for chunk {chunkPos}");
	}
	
	public void RequestUnloadChunk(Vector2Int chunkPos)
	{
		RequestUnloadChunkServerRpc(chunkPos);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RequestUnloadChunkServerRpc(Vector2Int chunkPos)
	{
		if(IsChunkInUse(chunkPos))
		{
			return;
		}
		
		InvisibleTilemapRemoveChunk(chunkPos);
		Bounds bounds = GetChunkBounds(chunkPos);
	}

	private bool IsChunkInUse(Vector2Int chunkPos)
	{
		return true;
	}

	private void InvisibleTilemapRemoveChunk(Vector2Int chunkPos)
	{
		
	}
	
	private Bounds GetChunkBounds(Vector2Int chunkPosition)
	{
		// Chunk size in tiles
		int chunkSize = ChunkManager.CHUNK_SIZE;

		// Calculate the world position of the bottom-left corner of the chunk
		Vector3 worldPosition = new Vector3(chunkPosition.x * chunkSize, chunkPosition.y * chunkSize, 0);

		// Define the bounds using the world position and chunk size
		Bounds bounds = new Bounds();

		// Center of the bounds is the middle of the chunk
		bounds.center = worldPosition + new Vector3(chunkSize / 2f, chunkSize / 2f, 0);

		// Size of the bounds is the chunk size in X and Y, with a small Z depth
		bounds.size = new Vector3(chunkSize, chunkSize, 1f);

		return bounds;
	}
}
