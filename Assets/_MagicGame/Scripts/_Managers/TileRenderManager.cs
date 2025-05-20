using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public struct TileVisibility
{
	public int Visibility; // 0 = transparent, 1 = opaque
	
	public TileVisibility(int visibility)
	{
		Visibility = visibility;
	}
}

public class TileRenderManager : NetworkBehaviour
{
	public static TileRenderManager Instance;

	[field: SerializeField] public Tilemap FloorTm { get; private set; }
	[field: SerializeField] public Tilemap WallTm { get; private set; }
	[field: SerializeField] public Tilemap OreTm { get; private set; }
	[field: SerializeField] public Tilemap FoliageTm { get; private set; }
	[field: SerializeField] public TerrainTileRenderer TerrainTileRenderer { get; private set; }
	[field: SerializeField] public UpperWallTm UpperWallTm { get; private set; }

	private void Awake()
	{
		WallTm.GetComponent<TilemapCollider2D>().enabled = false;

		Instance = this;
	}
	
	private void Start()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		WorldManager.Instance.OnBiomeTransitionStart += WorldManager_OnBiomeTransitionStart;
		WorldManager.Instance.OnBiomeTransitionEnd += WorldManager_OnBiomeTransitionEnd;
	}
	
	private void WorldManager_OnBiomeTransitionStart(object sender, EventArgs e)
	{
		WallTm.GetComponent<TilemapCollider2D>().enabled = false;
		UpperWallTm.EnableTilemapCollider(false);

		// Adding this because newly created tiles for some reason are not clearing with the naturally generated tiles... weird.
		TerrainTileRenderer.ClearAllTerrainTiles();
		FloorTm.ClearAllTiles();
		WallTm.ClearAllTiles();
		OreTm.ClearAllTiles();
		FoliageTm.ClearAllTiles();
	}

	private void WorldManager_OnBiomeTransitionEnd(object sender, EventArgs e)
    {
		WallTm.GetComponent<TilemapCollider2D>().enabled = true;
		UpperWallTm.EnableTilemapCollider(true);
	}

    private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Create a list of lists to hold all the different tile layers
		var allTileLayers = new List<List<TileGameData>>
		{
			e.Chunk.GroundTileGameDataList,
			e.Chunk.LiquidTileGameDataList,
			e.Chunk.FloorTileGameDataList,
			e.Chunk.WallTileGameDataList,
			e.Chunk.OreTileGameDataList,
			e.Chunk.FoliageTileGameDataList,
		};

		// Iterate through each list and set the tiles on the tilemap
		foreach (var tileLayer in allTileLayers)
		{
			foreach (TileGameData tile in tileLayer)
			{
				var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
				RenderTile(tilePosV3Int, tile.TileSO, tile.TileSO.TileType);
			}
		}
	}

	public bool HasTile(Vector3Int position, TileType tileType)
	{
        return tileType switch
        {
            TileType.Terrain => TerrainTileRenderer.HasTile(position),
            TileType.Floor => FloorTm.HasTile(position),
            TileType.Wall => WallTm.HasTile(position),
            TileType.Ore => OreTm.HasTile(position),
            TileType.Liquid => TerrainTileRenderer.HasTile(position),
            TileType.Foliage => FoliageTm.HasTile(position),
            _ => false,
        };
    }

	[Rpc(SendTo.ClientsAndHost)]
	public void HandleTileVisualClientRpc(Vector3Int pos, int syncTileId, TileType syncTileType, BiomeType biome)
	{
	    if (Player.LocalClientInstance.CurrentPlayerBiome.Value != biome) return;

	    TileSO tileToPlace = syncTileId >= 0 ? GameManager.Instance.GetTileSOFromID(syncTileId) : null;
	    RenderTile(pos, tileToPlace, syncTileType);

	    Lightmap.Instance.UpdateLightMap();
	}

	public void RenderTile(Vector3Int tilePos, TileSO tileSO, TileType tileType)
	{
		switch (tileType)
		{
			case TileType.Terrain:
				TerrainTileRenderer.SetTerrainTileData(tilePos, tileSO);
				break;
			case TileType.Liquid:
				TerrainTileRenderer.SetTerrainTileData(tilePos, tileSO);
				break;
			case TileType.Floor:
				FloorTm.SetTile(tilePos, tileSO);
				break;
			case TileType.Wall:
				WallTm.SetTile(tilePos, tileSO);
				break;
			case TileType.Ore: 
				OreTm.SetTile(tilePos, tileSO);
				if(tileSO == null) // When destroying ore, destroy the wall behind it
				{
					WallTm.SetTile(tilePos, tileSO);
				}
				break;
			case TileType.Foliage:
				FoliageTm.SetTile(tilePos, tileSO);
				break;
		}
		
		if(tileSO == null && (tileType == TileType.Wall || tileType == TileType.Ore))
		{
			UpperWallTm.DeleteUpperWallTile(tilePos);
		}
		else if (tileSO != null && (tileType == TileType.Wall || tileType == TileType.Ore))
		{
			UpperWallTm.TryToRenderSurroundingUpperWallTiles(tilePos);
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void DestroyTileServerRpc(Vector2Int tilePos, int tileId, BiomeType biome)
	{
		TileSO tileSO = GameManager.Instance.GetTileSOFromID(tileId);
		var tileList = GetTileListFromType(tileSO.TileType, tilePos, biome);
		if (tileList == null) return;
		Debug.Log($"Tilelist: {tileSO.TileType}, count {tileList.Count}");
		for (int i = tileList.Count - 1; i >= 0; i--)
		{
			if (tileList[i].TilePosition == tilePos)
			{
				var spawnPos = new Vector2(tileList[i].TilePosition.x + 0.5f, tileList[i].TilePosition.y + 0.5f);
				LootTable.SpawnLoot(tileSO.ItemDropTable, spawnPos, biome);
				ChunkManager.Instance.RemoveTileServerRpc(tileSO.TileType, tileList[i].TilePosition, biome);
				SoundManager.Instance.PlayOneShot(tileSO.DestroySound, spawnPos);
				Debug.Log($"Found tile to destroy: {tileList[i].TilePosition}");
				return;
			}
		}
		
		Debug.LogWarning($"Couldn't find tile to destroy: {tilePos}");
	}

	private List<TileGameData> GetTileListFromType(TileType tileType, Vector2Int tilePos, BiomeType biome)
	{
		var chunk = ChunkManager.Instance.GetChunkFromAnyWorldPos(tilePos, biome);
		Debug.Log($"Chunk: {chunk.ChunkPosition}");
		return tileType switch
		{
			TileType.Terrain => chunk.GroundTileGameDataList,
			TileType.Liquid => chunk.LiquidTileGameDataList,
			TileType.Floor => chunk.FloorTileGameDataList,
			TileType.Wall => chunk.WallTileGameDataList,
			TileType.Ore => chunk.OreTileGameDataList,
			TileType.Foliage => chunk.FoliageTileGameDataList,
			_ => null
		};
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		WorldManager.Instance.OnBiomeTransitionStart -= WorldManager_OnBiomeTransitionStart;
		WorldManager.Instance.OnBiomeTransitionEnd -= WorldManager_OnBiomeTransitionEnd;
	}
}
