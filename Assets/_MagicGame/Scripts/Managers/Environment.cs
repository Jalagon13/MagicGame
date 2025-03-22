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

public class TileHpData
{
	public Vector2Int TilePosition { get; private set; }
	public TileSO TileSO { get; private set; }
	public int CurrentTileHp { get; private set; }
	public bool IsDestroyed { get { return CurrentTileHp <= 0;  } }
	
	private BiomeType _biome;
	
	public TileHpData(TileSO tileSO, BiomeType biome, Vector2Int tilePosition)
	{
		TilePosition = tilePosition;
		TileSO = tileSO;
		CurrentTileHp = tileSO.MaxHitPoints;
		_biome = biome;
	}
	
	public void DamageTile(int amount)
	{
		CurrentTileHp -= amount;
		
		var spawnPos = new Vector2(TilePosition.x + 0.5f, TilePosition.y + 0.5f);
		SoundManager.Instance.PlayOneShot(TileSO.HitSound, spawnPos);
	}
	
	public void OnTileDestroy()
	{
		var spawnPos = new Vector2(TilePosition.x + 0.5f, TilePosition.y + 0.5f);
		GameManager.Instance.SpawnItem(TileSO.DropItem, 1, spawnPos, _biome);
		ChunkManager.Instance.RemoveTileDataFromChunk(TileSO, TilePosition, _biome);
		SoundManager.Instance.PlayOneShot(TileSO.DestroySound, spawnPos);
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
	
	public void AddTileVisData(Vector3Int pos, TileVisibility tileVisData)
	{
		TileVisibilityDict[pos] = tileVisData;
	}
	
	public void RemoveTileVisData(Vector3Int tilePosV3Int)
	{
		if(TileVisibilityDict.ContainsKey(tilePosV3Int))
		{
			TileVisibilityDict.Remove(tilePosV3Int);
		}
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void PlaceTileServerRpc(Vector3Int syncPos, int syncTileId, TileType syncTileType, BiomeType biome)
	{
		ChunkManager.Instance.AddTileDataToChunk((Vector2Int)syncPos, syncTileId, biome, syncTileType);
	}
	
	public void HitFloorTile(BiomeType biome, Vector2Int tilePos, int amount)
	{
		HitFloorTileServerRpc(biome, tilePos, amount);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void HitFloorTileServerRpc(BiomeType biome, Vector2Int tilePos, int amount)
	{
		var chunkGameData = ChunkManager.Instance.GetChunkFromAnyWorldPos(tilePos, biome);
		foreach (TileGameData tileGameData in chunkGameData.WallTileGameDataList)
		{
			if(tileGameData.TilePosition == tilePos)
			{
				// Found wall tile to hit
				HitTile(_biomeFloorTileHpDict, biome, tilePos, amount, tileGameData.TileSO);
				return;
			}
		}
		
		Debug.LogWarning($"Did not find floor tile to hit at {tilePos} in biome {biome}");
	}

	public void HitWallTile(BiomeType biome, Vector2Int tilePos, int amount)
	{
		HitWallTileServerRpc(biome, tilePos, amount);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void HitWallTileServerRpc(BiomeType biome, Vector2Int tilePos, int amount)
	{
		var chunkGameData = ChunkManager.Instance.GetChunkFromAnyWorldPos(tilePos, biome);
		foreach (TileGameData tileGameData in chunkGameData.WallTileGameDataList)
		{
			if(tileGameData.TilePosition == tilePos)
			{
				// Found wall tile to hit
				HitTile(_biomeWallTileHpDict, biome, tilePos, amount, tileGameData.TileSO);
				return;
			}
		}
	
		Debug.LogWarning($"Did not find wall tile to hit at {tilePos} in biome {biome}");
		TryToRemoveWallTileClientRpc(tilePos, biome);
	}

	public void TryToRemoveWallTile(Vector2Int position, BiomeType biomeToRemoveTileData)
	{
		TryToRemoveWallTileClientRpc(position, biomeToRemoveTileData);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void TryToRemoveWallTileClientRpc(Vector2Int position, BiomeType biomeToRemoveTileData)
	{
		if (Player.LocalClientInstance.CurrentPlayerBiome.Value != biomeToRemoveTileData || !ChunkManager.Instance.ObjectPositionInLoadedChunks(position)) return;

		WallTm.SetTile((Vector3Int)position, null);
		RemoveTileVisData((Vector3Int)position);
		Lightmap.Instance.UpdateLightMap();
	}

	private void HitTile(Dictionary<BiomeType, HashSet<TileHpData>> tileHpDict, BiomeType biome, Vector2Int tilePos, int amount, TileSO tileSO)
	{
		if(tileHpDict.ContainsKey(biome))
		{
			// Try to find tile to damage
			foreach (TileHpData tileHpData in tileHpDict[biome])
			{
				if(tileHpData.TilePosition == tilePos)
				{
					// Found tile to damage, so damage it
					DamageTile(tileHpDict, biome, amount, tileHpData);
					return;
				}
			}
			
			// Did not find tile to damage, create a new one, damage it
			DamageTile(tileHpDict, biome, amount, new TileHpData(tileSO, biome, tilePos));
		}
		else
		{
			// Biome does not exist, create it and add tile entry
			tileHpDict.Add(biome, new());
			DamageTile(tileHpDict, biome, amount, new TileHpData(tileSO, biome, tilePos));
			
			if(tileHpDict[biome].Count <= 0)
			{
				tileHpDict.Remove(biome);
			}
		}
	}
	
	private void DamageTile(Dictionary<BiomeType, HashSet<TileHpData>> tileHpDict, BiomeType biome, int amount, TileHpData tileToDamage)
	{
		tileToDamage.DamageTile(amount);
		
		if(tileToDamage.IsDestroyed)
		{
			tileToDamage.OnTileDestroy();
			
			// Check if tile exists in database, if so remove it
			foreach (TileHpData tileHpData in tileHpDict[biome].ToList())
			{
				if(tileHpData.TilePosition == tileToDamage.TilePosition)
				{
					// Found tile to destroy, delete it from the database
					tileHpDict[biome].Remove(tileHpData);
				}
			}
			
			Pathfinding.Instance.RemovePfWallTileServerRpc(tileToDamage.TilePosition, biome);
		}
		else
		{
			tileHpDict[biome].Add(tileToDamage);
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
			RemoveTileVisData(tilePosV3Int);
		}
			
		// loop through all wall tiles and set them on tilemap
		foreach(TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			WallTm.SetTile(tilePosV3Int, tile.TileSO);
			AddTileVisData(tilePosV3Int, new TileVisibility {Visibility = 1});
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		// Loop through all ground tiles and set null on tilemap
		foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			GroundTm.SetTile(tilePosV3Int, null);
			RemoveTileVisData(tilePosV3Int);
		}
		
		foreach (TileGameData tile in e.Chunk.WallTileGameDataList)
		{
			var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
			WallTm.SetTile(tilePosV3Int, null);
			RemoveTileVisData(tilePosV3Int);
		}
		
		Pathfinding.Instance.RequestUnloadChunk(e.Chunk.ChunkPosition, Player.LocalClientInstance.OwnerClientId, Player.LocalClientInstance.CurrentPlayerBiome.Value);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk -= ChunkManager_OnUnloadChunk;
		WorldManager.Instance.OnStartBiomeTransition -= ClearLocalTilemaps;
	}
}
