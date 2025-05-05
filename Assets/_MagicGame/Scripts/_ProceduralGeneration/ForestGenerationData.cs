using System;
using UnityEngine;

// Facilitates passing generation data to forest chunks
public class ForestGenerationData : MonoBehaviour
{
    [HideInInspector] public int[,] MostFrontRenderedTileMatrix = new int[ChunkManager.BIOME_SIDE_LENGTH, ChunkManager.BIOME_SIDE_LENGTH]; // Used for keeping track of tiles already placed in the map so far in the generation process so later gen steps can use it

    public string MapGenerationSeed;
    
    // Create new empty chunk data for this biome
    public void ResetForestData()
    {
        // Generate empty chunk data for population
        ChunkManager.Instance.GetChunksFromBiome(BiomeType.Forest).Clear();

        int chunkSideAmount = ChunkManager.BIOME_SIDE_LENGTH / ChunkManager.CHUNK_SIZE;
        for (int chunkX = 0; chunkX < chunkSideAmount; chunkX++)
        {
            for (int chunkY = 0; chunkY < chunkSideAmount; chunkY++)
            {
                Vector2Int chunkCoord = new(chunkX, chunkY);
                ChunkGameData chunkGameData = new(ChunkManager.CHUNK_SIZE, chunkCoord);
                ChunkManager.Instance.GetChunksFromBiome(BiomeType.Forest).Add(chunkCoord, chunkGameData);
            }
        }
    }

    public void SetTileData(int x, int y, TileSO tileSO)
    {
        Vector2Int pos = new Vector2Int(x, y);
        ChunkManager.Instance.GetChunkFromAnyWorldPos(pos, BiomeType.Forest).AddTileData(pos, tileSO);
        MostFrontRenderedTileMatrix[x, y] = GameManager.Instance.GetTileIdFromTileSO(tileSO);
    }

    public void SetObjectData(int pointX, int pointY, WorldObject treeObject, BiomeType biome, CardinalDirection direction)
    {
        ChunkManager.Instance.AddObjectDataToChunkServerRpc(new Vector2Int(pointX, pointY), GameManager.Instance.GetIDFromWorldObject(treeObject), biome, direction);
    }

    public bool IsInBounds(int x, int y)
    {
        int width = MostFrontRenderedTileMatrix.GetLength(0);
        int height = MostFrontRenderedTileMatrix.GetLength(1);
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}
