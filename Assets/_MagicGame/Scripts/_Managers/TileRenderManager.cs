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

	[field: SerializeField] public TopTile TopTilePrefab { get; private set; }
	[field: SerializeField] public Tilemap FloorTm { get; private set; }
	[field: SerializeField] public Tilemap WallTm { get; private set; }
	[field: SerializeField] public Tilemap OreTm { get; private set; }
	[field: SerializeField] public Tilemap FoliageTm { get; private set; }
	[field: SerializeField] public Tilemap LiquidTm { get; private set; }
	[field: SerializeField] public TerrainTileRenderer TerrainTileRenderer { get; private set; }

	private void Awake()
	{
		WallTm.GetComponent<TilemapCollider2D>().enabled = false;
		FoliageTm.GetComponent<TilemapCollider2D>().enabled = false;
		LiquidTm.GetComponent<TilemapCollider2D>().enabled = false;

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
		FoliageTm.GetComponent<TilemapCollider2D>().enabled = false;
		LiquidTm.GetComponent<TilemapCollider2D>().enabled = false;

		// Adding this because newly created tiles for some reason are not clearing with the naturally generated tiles... weird.
		TerrainTileRenderer.ClearAllTerrainTiles();
		FloorTm.ClearAllTiles();
		WallTm.ClearAllTiles();
		OreTm.ClearAllTiles();
		FoliageTm.ClearAllTiles();
		LiquidTm.ClearAllTiles();
	}

	private void WorldManager_OnBiomeTransitionEnd(object sender, EventArgs e)
    {
		WallTm.GetComponent<TilemapCollider2D>().enabled = true;
		FoliageTm.GetComponent<TilemapCollider2D>().enabled = true;
		LiquidTm.GetComponent<TilemapCollider2D>().enabled = true;
	}

    private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Create a list of lists to hold all the different tile layers
		var allTileLayers = new List<List<TileGameData>>
		{
			e.Chunk.GroundTileGameDataList,
			e.Chunk.FloorTileGameDataList,
			e.Chunk.WallTileGameDataList,
			e.Chunk.OreTileGameDataList,
			e.Chunk.FoliageTileGameDataList,
			e.Chunk.LiquidTileGameDataList
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

	public void ClearTopTiles()
	{
	    foreach (Transform child in WallTm.transform)
	    {
	        Destroy(child.gameObject);
	    }

		foreach (Transform child in OreTm.transform)
		{
			Destroy(child.gameObject);
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
            _ => false,
        };
    }

	public void RenderTile(Vector3Int tilePos, TileSO tileSO, TileType tileType)
	{
		switch (tileType)
		{
			case TileType.Terrain:
				// NTFS: Need to do this down the line
				TerrainTileRenderer.RenderTerrainTile(tilePos, tileSO);
				break;
			case TileType.Floor:
				FloorTm.SetTile(tilePos, tileSO);
				break;
			case TileType.Wall:
				WallTm.SetTile(tilePos, tileSO);
				if(tileSO == null) // When destroying wall, destroy the wall behind it
				{
					RefreshNearbyTopTiles(tilePos, WallTm);
				}
				break;
			case TileType.Ore: 
				OreTm.SetTile(tilePos, tileSO);
				if(tileSO == null) // When destroying ore, destroy the wall behind it
				{
					WallTm.SetTile(tilePos, tileSO);
					RefreshNearbyTopTiles(tilePos, OreTm);
				}
				break;
			case TileType.Foliage:
				FoliageTm.SetTile(tilePos, tileSO);
				break;
			case TileType.Liquid:
				LiquidTm.SetTile(tilePos, tileSO);
				break;
		}
	}
	
	public void ExecuteTopTilePassthrough()
	{
		Debug.Log("ExecuteTopTilePassthrough");
	    foreach (Vector3Int pos in WallTm.cellBounds.allPositionsWithin)
	    {
	        if (!WallTm.HasTile(pos)) continue;
	
			if(!WallTm.HasTile(pos + Vector3Int.up))
			{
				if(OreTm.HasTile(pos))
				{
					TileSO oreTileSO = GameManager.Instance.GetTileSOFromTileBase(OreTm.GetTile(pos));
					HandleTopWallTiles(pos, oreTileSO, OreTm);
				}

				TileSO tileSO = GameManager.Instance.GetTileSOFromTileBase(WallTm.GetTile(pos));
				HandleTopWallTiles(pos, tileSO, WallTm);
			}
	    }
	}
	
    public void HandleTopWallTiles(Vector3Int botTilePosition, TileSO tileSO, Tilemap tilemap)
    {
		if(tileSO != null)
		{
			if (tileSO.TopTileSingle == null) return; // This is temp for now

			Vector3Int topTilePosition = botTilePosition + Vector3Int.up;
			TileBase topTile = tilemap.GetTile(topTilePosition);

			if (topTile != null)
			{
				int topTileId = GameManager.Instance.GetTileIdFromTileBase(topTile);
				int botTileId = GameManager.Instance.GetTileIdFromTileBase(tileSO);

				if (topTileId == botTileId)
				{
					UpdateNearbyTopTiles(botTilePosition);
					return;
				}
			}

			TopTile tt = Instantiate(TopTilePrefab, topTilePosition, Quaternion.identity);
			tt.gameObject.transform.SetParent(tilemap.gameObject.transform);
			tt.Initialize(tileSO, botTilePosition);
		}
		else
		{
			RefreshNearbyTopTiles(botTilePosition, tilemap);
		}
	}
	
	public void RefreshNearbyTopTiles(Vector3Int botTilePosition, Tilemap tilemap)
	{
		UpdateNearbyTopTiles(botTilePosition);

		Vector3Int[] directions = new Vector3Int[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
		foreach (Vector3Int direction in directions)
		{
			Vector3Int neighborPos = botTilePosition + direction;

			if (HasTile(neighborPos, TileType.Wall))
			{
				HandleTopWallTiles(neighborPos, GameManager.Instance.GetTileSOFromTileBase(WallTm.GetTile(neighborPos)), tilemap);
			}
		}
	}
	
	public void UpdateNearbyTopTiles(Vector3Int botTilePosition)
	{
	    Vector3Int[] directions = new Vector3Int[]
	    {
	        new Vector3Int(-1, 1, 0),  // Top-left
	        new Vector3Int(0, 1, 0),   // Top
	        new Vector3Int(1, 1, 0),   // Top-right
	        new Vector3Int(-1, 0, 0),  // Left
	        new Vector3Int(1, 0, 0),   // Right
	        new Vector3Int(-1, -1, 0), // Bottom-left
	        new Vector3Int(0, -1, 0),  // Bottom
	        new Vector3Int(1, -1, 0),   // Bottom-right
	        new Vector3Int(0, 0, 0)
	    };

	    foreach (var offset in directions)
	    {
	        Vector3Int neighborPos = botTilePosition + offset;
	        Collider2D[] colliders = Physics2D.OverlapPointAll(new Vector2(neighborPos.x + 0.5f, neighborPos.y + 0.5f));
	        foreach (var collider in colliders)
	        {
	            TopTile topTileFound = collider.GetComponent<TopTile>();
	            if (topTileFound != null)
	            {
	                topTileFound.UpdateSelf();
	            }
	        }
	    }
	}

    [Rpc(SendTo.Server, RequireOwnership = false)]
	public void DestroyTileServerRpc(Vector2Int tilePos, int tileId, BiomeType biome)
	{
		TileSO tileSO = GameManager.Instance.GetTileSOFromID(tileId);
		var tileList = GetTileListFromType(tileSO.TileType, tilePos, biome);
		if (tileList == null) return;

		for (int i = tileList.Count - 1; i >= 0; i--)
		{
			if (tileList[i].TilePosition == tilePos)
			{
				var spawnPos = new Vector2(tileList[i].TilePosition.x + 0.5f, tileList[i].TilePosition.y + 0.5f);
				LootTable.SpawnLoot(tileSO.ItemDropTable, spawnPos, biome);
				ChunkManager.Instance.RemoveTileServerRpc(tileSO.TileType, tileList[i].TilePosition, biome);
				SoundManager.Instance.PlayOneShot(tileSO.DestroySound, spawnPos);
				break;
			}
		}
	}

	private List<TileGameData> GetTileListFromType(TileType tileType, Vector2Int tilePos, BiomeType biome)
	{
		var chunk = ChunkManager.Instance.GetChunkFromAnyWorldPos(tilePos, biome);

		return tileType switch
		{
			TileType.Terrain => chunk.GroundTileGameDataList,
			TileType.Floor => chunk.FloorTileGameDataList,
			TileType.Wall => chunk.WallTileGameDataList,
			TileType.Ore => chunk.OreTileGameDataList,
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
