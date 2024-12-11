using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnvironmentFileData 
{
    public List<WorldAssetFileData> WorldAssets = new();
    public List<ChunkFileData> WorldChunks = new();
}

[System.Serializable]
public class TileFileData // For Serialization
{
    public int TileId;
    public Vector2Int Pos; // Could be position in chunk or in the world not sure yet
    public TileType TileType;
}

[System.Serializable]
public class ChunkFileData // For Serialization
{
    public Vector2Int ChunkPosition;
    public List<TileFileData> GroundTiles;
    public List<TileFileData> WallTiles;
    public int Size; // Width and Height
}

[System.Serializable]
public class WorldAssetFileData // For Serialization
{
    public int WorldAssetId;
    public Vector2Int Pos;
}