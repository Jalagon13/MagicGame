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

public class TileHpData
{
	public Vector2Int TilePosition;
	public TileSO TileSO { get; private set; }
	public int CurrentTileHp { get; private set; }
	
	public TileHpData(TileSO tileSO)
	{
		TileSO = tileSO;
		CurrentTileHp = tileSO.MaxHitPoints;
	}
	
	public void DamageTile(int amount)
	{
		CurrentTileHp -= amount;
		
		if(CurrentTileHp <= 0)
		{
			// Destroy logic
		}
	}
}

public class Environment : NetworkBehaviour
{
	public static Environment Instance;

	[field: SerializeField] public Tilemap GroundTm { get; private set; }
	[field: SerializeField] public Tilemap FloorTm { get; private set; }
	[field: SerializeField] public Tilemap WallTm { get; private set; }
	public Dictionary<Vector3Int, TileVisibility> TileVisibilityDict { get; private set; } = new();

	private Dictionary<BiomeType, HashSet<TileHpData>> _biomeFloorTileHpDict = new();
	private Dictionary<BiomeType, HashSet<TileHpData>> _biomeWallTileHpDict = new();

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
	
	public void HitFloorTile(BiomeType biome, Vector2Int tilePos, int amount)
	{
		HitTile(_biomeFloorTileHpDict, biome, tilePos, amount);
	}

	public void HitWallTile(BiomeType biome, Vector2Int tilePos, int amount)
	{
		HitTile(_biomeWallTileHpDict, biome, tilePos, amount);
	}
	
	private void HitTile(Dictionary<BiomeType, HashSet<TileHpData>> tileHpDict, BiomeType biome, Vector2Int tilePos, int amount)
	{
		if(tileHpDict.ContainsKey(biome))
		{
			foreach (TileHpData tileHpData in tileHpDict[biome])
			{
				if(tileHpData.TilePosition == tilePos)
				{
					// Found tile to damage, so damage it
					tileHpData.DamageTile(amount);
					return;
				}
			}
			
			// Did not find tile to damage, add it and damage it
			var chunkGameData = ChunkManager.Instance.GetChunkFromAnyWorldPos(tilePos, biome);
			
			
			tileHpDict[biome].Add(new TileHpData());
		}
		else
		{
			tileHpDict.Add(biome, new());
			
			var chunkGameData = ChunkManager.Instance.GetChunkFromAnyWorldPos(tilePos, biome);
			var tile = chunkGameData.
			
			tileHpDict[biome].Add(new TileHpData());
		}
	}

	private void ClearLocalTilemaps(object sender, EventArgs e)
	{
		// Adding this because newly created tiles for some reason are not clearing with the naturally generated tiles... weird.
		GroundTm.ClearAllTiles();
		FloorTm.ClearAllTiles();
		WallTm.ClearAllTiles();
	}

	private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			GroundTm.SetTile(tilePosV3Int, tile.TileSO);
		}
			
		// loop through all wall tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			WallTm.SetTile(tilePosV3Int, tile.TileSO);
			
			// Populate Dicionary with tile visibility
			if(!TileVisibilityDict.ContainsKey(tilePosV3Int))
			{
				// var isOpaque = e.Chunk.WallTileGameDataList.Exists(wallTile => wallTile.TilePosition == tile.TilePosition);
				TileVisibilityDict.Add(tilePosV3Int, new TileVisibility {Visibility = 1/* isOpaque ? 1 : 0 */});
			}
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set null on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			GroundTm.SetTile(tilePosV3Int, null);
			
			if(TileVisibilityDict.ContainsKey(tilePosV3Int))
			{
				TileVisibilityDict.Remove(tilePosV3Int);
			}
		}
		
		foreach (TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			WallTm.SetTile(tilePosV3Int, null);
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
		if(Player.LocalClientInstance.CurrentBiome.Value != biome || !ObjectPositionInLoadedChunks((Vector2Int)syncPos)) return;
		
		TileSO tileToPlace = GameManager.Instance.GetTileSOFromID(syncTileId);

		// Chunk is loaded visually, therefore visually update whatever tile wants to be updated
		switch(syncTileType)
		{
			case TileType.Ground:
				break;
			case TileType.Floor:
				FloorTm.SetTile(syncPos, tileToPlace);
				break;
			case TileType.Wall:
				WallTm.SetTile(syncPos, tileToPlace);
					
				TileVisibilityDict[syncPos] = new TileVisibility {Visibility = 1};
					
				Lightmap.Instance.UpdateLightMap();
				break;
		}
	}
	
	private bool ObjectPositionInLoadedChunks(Vector2Int position)
	{
		var minLoadedTilePos = ChunkManager.Instance.MinLoadedTilePosition;
		var maxLoadedTilePos = ChunkManager.Instance.MaxLoadedTilePosition;

		// Check if the position is within the bounds
		return position.x >= minLoadedTilePos.x && position.x <= maxLoadedTilePos.x &&
			   position.y >= minLoadedTilePos.y && position.y <= maxLoadedTilePos.y;
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk -= ChunkManager_OnUnloadChunk;
		WorldManager.Instance.OnStartBiomeTransition -= ClearLocalTilemaps;
	}
}
