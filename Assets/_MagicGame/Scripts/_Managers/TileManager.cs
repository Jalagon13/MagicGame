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

public class TileManager : NetworkBehaviour
{
	public static TileManager Instance;

	[field: SerializeField] public Tilemap FloorTm { get; private set; }
	[field: SerializeField] public Tilemap WallTm { get; private set; }
	[field: SerializeField] public Tilemap OreTm { get; private set; }
	[field: SerializeField] public Tilemap FoliageTm { get; private set; }
	[field: SerializeField] public TerrainTileRenderer TerrainTileRenderer { get; private set; }
	[field: SerializeField] public UpperWallTm UpperWallTm { get; private set; }
	[field: SerializeField] public TileDestructionFeedbacks TileDestructionFeedbacks { get; private set; }


	private void Awake()
	{
		WallTm.GetComponent<TilemapCollider2D>().enabled = false;

		Instance = this;
	}
	
	private void Start()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		GameWorld.Instance.OnBiomeTransitionStart += WorldManager_OnBiomeTransitionStart;
		GameWorld.Instance.OnBiomeTransitionEnd += WorldManager_OnBiomeTransitionEnd;
	}
	
	private void WorldManager_OnBiomeTransitionStart(object sender, EventArgs e)
	{
		WallTm.GetComponent<TilemapCollider2D>().enabled = false;
		OreTm.GetComponent<TilemapRenderer>().enabled = false;
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
		OreTm.GetComponent<TilemapRenderer>().enabled = true;
		UpperWallTm.EnableTilemapCollider(true);
	}

    private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Create a list of lists to hold all the different tile layers
		var allTileLayers = new List<List<TileGameData>>
		{
			e.Chunk.GetTileList(TileType.Terrain),
			e.Chunk.GetTileList(TileType.Liquid),
			e.Chunk.GetTileList(TileType.Floor),
			e.Chunk.GetTileList(TileType.Wall),
			e.Chunk.GetTileList(TileType.Ore),
			e.Chunk.GetTileList(TileType.Foliage),
		};

		// Iterate through each list and set the tiles on the tilemap
		foreach (var tileLayer in allTileLayers)
		{
			foreach (TileGameData tile in tileLayer)
			{
				var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
				RenderTile(tilePosV3Int, tile.TileSO, tile.TileSO.TileType, false);
			}
		}
	}

	public bool HasTile(Vector3Int position, TileType tileType, out TileDataSO tileSO)
	{
		tileSO = null;

		switch (tileType)
		{
			case TileType.Terrain:
			case TileType.Liquid:
				if (TerrainTileRenderer.HasTile(position))
				{
					tileSO = TerrainTileRenderer.GetTileData(position);
					return true;
				}
				break;
			case TileType.Floor:
				if (FloorTm.HasTile(position))
				{
					tileSO = FloorTm.GetTile<TileDataSO>(position);
					return true;
				}
				break;
			case TileType.Wall:
				if (WallTm.HasTile(position))
				{
					tileSO = WallTm.GetTile<TileDataSO>(position);
					return true;
				}
				break;
			case TileType.Ore:
				if (OreTm.HasTile(position))
				{
					tileSO = OreTm.GetTile<TileDataSO>(position);
					return true;
				}
				break;
			case TileType.Foliage:
				if (FoliageTm.HasTile(position))
				{
					tileSO = FoliageTm.GetTile<TileDataSO>(position);
					return true;
				}
				break;
		}

		return false;
	}

	[Rpc(SendTo.ClientsAndHost)]
	public void HandleTileVisualClientRpc(Vector3Int pos, ushort syncTileId, TileType syncTileType, BiomeType biome, bool playDestroyFeedbacks)
	{
	    if (Player.Instance.CurrentBiome.Value != biome) return;

		if(syncTileId == GameDataRegistry.INVALID_ID)
		{
		    RenderTile(pos, null, syncTileType, playDestroyFeedbacks);
		}
		else
		{
			TileDataSO tileToPlace = GameDataRegistry.Instance.GetTileDataFromTileId(syncTileId);
			RenderTile(pos, tileToPlace, syncTileType, playDestroyFeedbacks);
		}

	    Lightmap.Instance.UpdateLightMap();
	}

	public void RenderTile(Vector3Int tilePos, TileDataSO tileSO, TileType tileType, bool playDestroyFeedbacks)
	{
		TileDataSO previousTile = null;
	
		switch (tileType)
		{
			case TileType.Terrain:
			case TileType.Liquid:
				previousTile = TerrainTileRenderer.GetTileData(tilePos);
				TerrainTileRenderer.SetTerrainTileData(tilePos, tileSO);
				break;
			case TileType.Floor:
				previousTile = FloorTm.GetTile<TileDataSO>(tilePos);
				FloorTm.SetTile(tilePos, tileSO);
				break;
			case TileType.Wall:
				previousTile = WallTm.GetTile<TileDataSO>(tilePos);
				WallTm.SetTile(tilePos, tileSO);
				break;
			case TileType.Ore: 
				previousTile = OreTm.GetTile<TileDataSO>(tilePos);
				OreTm.SetTile(tilePos, tileSO);
				if(tileSO == null) // When destroying ore, destroy the wall behind it
				{
					WallTm.SetTile(tilePos, tileSO);
				}
				break;
			case TileType.Foliage:
				previousTile = FoliageTm.GetTile<TileDataSO>(tilePos);
				FoliageTm.SetTile(tilePos, tileSO);
				break;
		}
		
		if(tileSO == null && (tileType == TileType.Wall || tileType == TileType.Ore))
		{
			UpperWallTm.DeleteUpperWallTile(tilePos);
		}
		else if (tileSO != null && (tileType == TileType.Wall || tileType == TileType.Ore) && !GameWorld.Instance.IsLoadingBiome)
		{
			UpperWallTm.TryToRenderSurroundingUpperWallTiles(tilePos);
		}
		
		if(playDestroyFeedbacks && previousTile != null && tileSO == null)
		{
		    // Spawn destroy feedbacks prefab that automatically deletes itself when done playing
		    var go = Instantiate(TileDestructionFeedbacks.gameObject, tilePos, Quaternion.identity);
		    go.GetComponent<TileDestructionFeedbacks>().PlayDestroyFeedbacks(previousTile);
		}
	}

	public void DestroyTile(Vector2Int tilePos, ushort tileId, BiomeType biome, bool playDestroyFeedbacks)
	{
		TileDataSO tileSO = GameDataRegistry.Instance.GetTileDataFromTileId(tileId);
		var tileList = GetTileListFromType(tileSO.TileType, tilePos, biome);
		
		if (tileList == null)
		{
		    Debug.LogError($"tileList for {tileSO.TileType} is null");
		    return;
		}

		for (int i = tileList.Count - 1; i >= 0; i--)
		{
			if (tileList[i].TilePosition == tilePos)
			{
				var spawnPos = new Vector2(tileList[i].TilePosition.x + 0.5f, tileList[i].TilePosition.y + 0.5f);
				LootTable.SpawnLoot(tileSO.ItemDropTable, spawnPos, biome);
				ChunkManager.Instance.RemoveTileServerRpc(tileSO.TileType, tileList[i].TilePosition, biome, playDestroyFeedbacks);
				SoundManager.Instance.PlayOneShot(tileSO.DestroySound, spawnPos);
				return;
			}
		}
	}

	private List<TileGameData> GetTileListFromType(TileType tileType, Vector2Int tilePos, BiomeType biome)
	{
		var chunk = ChunkManager.Instance.GetChunkFromAnyWorldPos(tilePos, biome);
		return chunk.GetTileList(tileType);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		GameWorld.Instance.OnBiomeTransitionStart -= WorldManager_OnBiomeTransitionStart;
		GameWorld.Instance.OnBiomeTransitionEnd -= WorldManager_OnBiomeTransitionEnd;
	}
}
