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
}

public class TileManager : NetworkBehaviour
{
	public static TileManager Instance;

	[field: SerializeField] public TopTile TopTilePrefab { get; private set; }
	[field: SerializeField] public Tilemap GroundTm { get; private set; }
	[field: SerializeField] public Tilemap FloorTm { get; private set; }
	[field: SerializeField] public Tilemap WallTm { get; private set; }
	[field: SerializeField] public Tilemap OreTm { get; private set; }
	public Dictionary<Vector3Int, TileVisibility> TileVisibilityDict { get; private set; } = new();

	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
		WorldManager.Instance.OnBiomeTransitionStart += ClearLocalTilemaps;
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
	
	public void AddTileVisibilityData(Vector3Int pos, TileVisibility tileVisData)
	{
		TileVisibilityDict[pos] = tileVisData;
	}
	
	public void RemoveTileVisibilityData(Vector3Int tilePosV3Int)
	{
		if(TileVisibilityDict.ContainsKey(tilePosV3Int))
		{
			TileVisibilityDict.Remove(tilePosV3Int);
		}
	}

	public bool HasTile(Vector3Int position, TileType tileType)
	{
        return tileType switch
        {
            TileType.Ground => GroundTm.HasTile(position),
            TileType.Floor => FloorTm.HasTile(position),
            TileType.Wall => WallTm.HasTile(position),
            TileType.Ore => OreTm.HasTile(position),
            _ => false,
        };
    }

	public void SetLocalTile(Vector3Int tilePos, TileSO tileSO, TileType tileType)
	{
		switch (tileType)
		{
			case TileType.Ground:
				// NTFS: Need to do this down the line
				GroundTm.SetTile(tilePos, tileSO);
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
				LootTable.SpawnLoot(tileSO.Table, spawnPos, biome);
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
			TileType.Ground => chunk.GroundTileGameDataList,
			TileType.Floor => chunk.FloorTileGameDataList,
			TileType.Wall => chunk.WallTileGameDataList,
			TileType.Ore => chunk.OreTileGameDataList,
			_ => null
		};
	}
	
	private void ClearLocalTilemaps(object sender, EventArgs e)
	{
		// Adding this because newly created tiles for some reason are not clearing with the naturally generated tiles... weird.
		GroundTm.ClearAllTiles();
		FloorTm.ClearAllTiles();
		WallTm.ClearAllTiles();
		OreTm.ClearAllTiles();
	}

	private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, tile.TileSO, tile.TileSO.TileType);
			RemoveTileVisibilityData(tilePosV3Int);
		}
		
		// loop through all floor tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.FloorTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, tile.TileSO, tile.TileSO.TileType);
		}
			
		// loop through all wall tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, tile.TileSO, tile.TileSO.TileType);
			AddTileVisibilityData(tilePosV3Int, new TileVisibility {Visibility = 1});
		}
		
		// loop through all ore tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.OreTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, tile.TileSO, tile.TileSO.TileType);
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, null, TileType.Ground);
			RemoveTileVisibilityData(tilePosV3Int);
		}
		
		foreach (TileGameData tile in e.Chunk.FloorTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, null, TileType.Floor);
			RemoveTileVisibilityData(tilePosV3Int);
		}
		
		foreach (TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, null, TileType.Wall);
			RemoveTileVisibilityData(tilePosV3Int);
		}

		foreach (TileGameData tile in e.Chunk.OreTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			SetLocalTile(tilePosV3Int, null, TileType.Ore);
			RemoveTileVisibilityData(tilePosV3Int);
		}

		Pathfinding.Instance.RequestUnloadChunk(e.Chunk.ChunkPosition, Player.LocalClientInstance.OwnerClientId, Player.LocalClientInstance.CurrentPlayerBiome.Value);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk -= ChunkManager_OnUnloadChunk;
		WorldManager.Instance.OnBiomeTransitionStart -= ClearLocalTilemaps;
	}
}
