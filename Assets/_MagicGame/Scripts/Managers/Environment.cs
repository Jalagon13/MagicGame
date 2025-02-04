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

	[field: SerializeField] public TilemapData GroundTmData { get; private set; }
	[field: SerializeField] public TilemapData FloorTmData { get; private set; }
	[field: SerializeField] public TilemapData WallTmData { get; private set; }

	public Dictionary<Vector3Int, TileVisibility> TileVisibilityDict { get; private set; } = new();

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
		GroundTmData.GetTilemap().ClearAllTiles();
		FloorTmData.GetTilemap().ClearAllTiles();
		WallTmData.GetTilemap().ClearAllTiles();
	}

	private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			GroundTmData.GetTilemap().SetTile(tilePosV3Int, tile.TileSO);
			
			// Populate Dicionary with tile visibility
			if(!TileVisibilityDict.ContainsKey(tilePosV3Int))
			{
				var isOpaque = e.Chunk.WallTileGameDataList.Exists(wallTile => wallTile.TilePosition == tile.TilePosition);
				TileVisibilityDict.Add(tilePosV3Int, new TileVisibility {Visibility = isOpaque ? 1 : 0});
			}
		}
			
		// loop through all wall tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			WallTmData.GetTilemap().SetTile(tilePosV3Int, tile.TileSO);
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set null on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			GroundTmData.GetTilemap().SetTile(tilePosV3Int, null);
			
			if(TileVisibilityDict.ContainsKey(tilePosV3Int))
			{
				TileVisibilityDict.Remove(tilePosV3Int);
			}
		}
		
		foreach (TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			WallTmData.GetTilemap().SetTile(tilePosV3Int, null);
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
	private void AddTileDataServerRpc(Vector3Int syncPos, byte syncTileId, TileType syncTileType, BiomeType biome)
	{
		// Debug.Log("Server is adding tile data to official world data");
		ChunkManager.Instance.AddWallTileDataToChunk((Vector2Int)syncPos, syncTileId, biome);
		
		HandleTileVisualClientRpc(syncPos, syncTileId, syncTileType, biome);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void HandleTileVisualClientRpc(Vector3Int syncPos, byte syncTileId, TileType syncTileType, BiomeType biome)
	{
		if(Player.LocalClientInstance.CurrentBiome.Value != biome) return;
	
		TileSO tileToPlace = GameManager.Instance.GetTileSOFromID(syncTileId);

		if(GroundTmData.GetTilemap().HasTile(syncPos))
		{
			// Chunk is loaded visually, therefore visually update whatever tile wants to be updated
			switch(syncTileType)
			{
				case TileType.Ground:
					break;
				case TileType.Floor:
					FloorTmData.GetTilemap().SetTile(syncPos, tileToPlace);
					break;
				case TileType.Wall:
					WallTmData.GetTilemap().SetTile(syncPos, tileToPlace);
					
					TileVisibilityDict[syncPos] = new TileVisibility {Visibility = 1};
					
					Lightmap.Instance.UpdateLightMap();
					break;
			}
		}
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk -= ChunkManager_OnUnloadChunk;
		WorldManager.Instance.OnStartBiomeTransition -= ClearLocalTilemaps;
	}
}
