using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Class to store chunk data including tiles and world assets
public class ChunkGameData
{
    public Vector2Int ChunkPosition { get; private set; }
    public List<TileGameData> GroundTileGameDataList;
    public List<TileGameData> WallTileGameDataList;// Note to future self: Make floor serializable too later
    public List<WorldAssetGameData> WorldAssetGameDataList;
    public int Size { get; private set; }

    public ChunkGameData(int chunkSize, Vector2Int chunkPosition)
    {
        Size = chunkSize;
        ChunkPosition = chunkPosition;
        GroundTileGameDataList = new();
        WallTileGameDataList = new();
        WorldAssetGameDataList = new();
    }
	
    // When a tile is destroyed, delete the tile data in chunk
    public void RemoveWallTileData(Vector2Int position)
    {
        // Loop through all Wall tiles and find tile at position
        foreach (TileGameData tile in WallTileGameDataList)
        {
            // If position is found
            if(tile.TilePosition == position)
            {
                // Delete data and return
                WallTileGameDataList.Remove(tile);
                return;
            }
        }
		
        // If tile data to delete does not exist, log error it
        Debug.LogError("Trying to delete tile data that is not there");
    }
	
    // When a tile is placed, add tile data in chunk
    public void AddWallTileData(Vector2Int position, TileSO tile)
    {
        TileGameData tileToAdd = new(tile, position);
		
        WallTileGameDataList.Add(tileToAdd);
    }

    public void AddWorldAssetData(Vector2Int position, WorldObject worldObject)
    {
        WorldAssetGameData worldAssetToAdd = new(worldObject, position);
		
        WorldAssetGameDataList.Add(worldAssetToAdd);
    }

    public void RemoveWorldAssetData(Vector2Int position)
    {
        foreach (WorldAssetGameData assetGameData in WorldAssetGameDataList)
        {
            if(assetGameData.Position == position)
            {
                WorldAssetGameDataList.Remove(assetGameData);
                return;
            }
        }
    }
}

public struct WorldAssetGameData
{
    public WorldObject Asset { get; private set; }
    public Vector2Int Position { get; private set; }
	
    public WorldAssetGameData(WorldObject worldObject, Vector2Int position)
    {
        Asset = worldObject;
        Position = position;
    }
}

// Data used in the game to store tile information (versus tile information stored on file)
public struct TileGameData
{
    public TileSO TileSO { get; private set; }
    public Vector2Int TilePosition { get; private set; }

    public TileGameData(TileSO tileBase, Vector2Int position)
    {
        TileSO = tileBase;
        TilePosition = position;
    }
}