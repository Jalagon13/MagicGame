using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct TileVisibility
{
	public int Visibility; // 0 = transparent, 1 = opaque
}

public class Environment : NetworkBehaviour
{
	public static Environment Instance;

	[SerializeField] private TilemapData _groundTilemapData;
	[SerializeField] private TilemapData _floorTilemapData;
	[SerializeField] private TilemapData _wallTilemapData;

	private Dictionary<Vector3Int, TileVisibility> _tileVisibilityDictionary = new();

	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
		WorldManager.Instance.OnStartBiomeTransition += ClearLocalTilemaps;
	}

	private void ClearLocalTilemaps(object sender, EventArgs e)
	{
		// Adding this because newly created tiles for some reason are not clearing with the naturally generated tiles... weird.
		_groundTilemapData.GetTilemap().ClearAllTiles();
		_floorTilemapData.GetTilemap().ClearAllTiles();
		_wallTilemapData.GetTilemap().ClearAllTiles();
	}

	private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			_groundTilemapData.GetTilemap().SetTile(tilePosV3Int, tile.TileSO);
			
			// Populate Dicionary with tile visibility
			if(!_tileVisibilityDictionary.ContainsKey(tilePosV3Int))
			{
				var isOpaque = e.Chunk.WallTileGameDataList.Exists(wallTile => wallTile.TilePosition == tile.TilePosition);
				_tileVisibilityDictionary.Add(tilePosV3Int, new TileVisibility {Visibility = isOpaque ? 1 : 0});
			}
		}
			
		// loop through all wall tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			_wallTilemapData.GetTilemap().SetTile(tilePosV3Int, tile.TileSO);
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set null on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			_groundTilemapData.GetTilemap().SetTile(tilePosV3Int, null);
			
			if(_tileVisibilityDictionary.ContainsKey(tilePosV3Int))
			{
				_tileVisibilityDictionary.Remove(tilePosV3Int);
			}
		}
		
		foreach (TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			_wallTilemapData.GetTilemap().SetTile(tilePosV3Int, null);
		}
		
		Pathfinding.Instance.RequestUnloadChunk(e.Chunk.ChunkPosition, Player.LocalClientInstance.OwnerClientId, Player.LocalClientInstance.CurrentBiome.Value);
	}
	
	// Handles placing the visual of the tile, NOT the tile data that is being synced
	public void PlaceTile(Vector3Int pos, TileSO wallTile, TileType syncTileType, BiomeType environment)
	{
		// Debug.Log("Some Client is placing a tile");
		byte syncTileId = GameManager.Instance.GetByteIDFromTileObjectSO(wallTile);
		
		AddTileDataServerRpc(pos, syncTileId, syncTileType, environment);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void AddTileDataServerRpc(Vector3Int syncPos, byte syncTileId, TileType syncTileType, BiomeType environment)
	{
		// Debug.Log("Server is adding tile data to official world data");
		ChunkManager.Instance.AddWallTileDataToChunk((Vector2Int)syncPos, syncTileId, environment);
		
		HandleTileVisualClientRpc(syncPos, syncTileId, syncTileType);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void HandleTileVisualClientRpc(Vector3Int syncPos, byte syncTileId, TileType syncTileType)
	{
		// Debug.Log("Distributing visual placement information for each client to decide if it is worth placing based on chunks being loaded");
		TileSO tileToPlace = GameManager.Instance.GetTileSOFromID(syncTileId);

		// If ground tilemap has a tile at this location, that means the chunk is loaded and is able to accept visual changes
		if(_groundTilemapData.GetTilemap().HasTile(syncPos))
		{
			// Chunk is loaded visually, therefore visually update whatever tile wants to be updated
			switch(syncTileType)
			{
				case TileType.Ground:
					break;
				case TileType.Floor:
					_floorTilemapData.GetTilemap().SetTile(syncPos, tileToPlace);
					break;
				case TileType.Wall:
					_wallTilemapData.GetTilemap().SetTile(syncPos, tileToPlace);
					break;
			}
		}
	}

	private void ClearTilemaps()
	{
		_groundTilemapData.GetTilemap().ClearAllTiles();
		_floorTilemapData.GetTilemap().ClearAllTiles();
		_wallTilemapData.GetTilemap().ClearAllTiles();
	}
	
	public TilemapData GetGroundTilemapData()
	{
		return _groundTilemapData;
	}
	
	public TilemapData GetFloorTilemapData()
	{
		return _floorTilemapData;
	}
	
	public TilemapData GetWallTilemapData()
	{
		return _wallTilemapData;
	}
	
	public Dictionary<Vector3Int, TileVisibility> GetTileVisibilityDictionary()
	{
		return _tileVisibilityDictionary;
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk -= ChunkManager_OnUnloadChunk;
		WorldManager.Instance.OnStartBiomeTransition -= ClearLocalTilemaps;
	}
}
