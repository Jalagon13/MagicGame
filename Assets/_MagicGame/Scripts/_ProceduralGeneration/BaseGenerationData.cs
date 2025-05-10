using UnityEngine;

public abstract class BaseGenerationData : MonoBehaviour
{

    [HideInInspector] public int[,] MostFrontRenderedTileMatrix = new int[ChunkManager.BIOME_SIDE_LENGTH, ChunkManager.BIOME_SIDE_LENGTH];
    [HideInInspector] public string MapGenerationSeed;
    
    protected abstract BiomeType _biomeType { get; }

    public virtual void ResetData()
    {
        ChunkManager.Instance.GetChunksFromBiome(_biomeType).Clear();

        int chunkSideAmount = ChunkManager.BIOME_SIDE_LENGTH / ChunkManager.CHUNK_SIZE;
        for (int chunkX = 0; chunkX < chunkSideAmount; chunkX++)
        {
            for (int chunkY = 0; chunkY < chunkSideAmount; chunkY++)
            {
                Vector2Int chunkCoord = new(chunkX, chunkY);
                ChunkGameData chunkGameData = new(ChunkManager.CHUNK_SIZE, chunkCoord);
                ChunkManager.Instance.GetChunksFromBiome(_biomeType).Add(chunkCoord, chunkGameData);
            }
        }
    }

    public virtual void SetTileData(int x, int y, TileSO tileSO)
    {
        Vector2Int pos = new Vector2Int(x, y);
        ChunkManager.Instance.GetChunkFromAnyWorldPos(pos, _biomeType).AddTileData(pos, tileSO);
        MostFrontRenderedTileMatrix[x, y] = GameManager.Instance.GetTileIdFromTileSO(tileSO);
    }

    public virtual void SetWorldObjectData(int x, int y, WorldObject obj, CardinalDirection dir)
    {
        ChunkManager.Instance.AddObjectDataToChunkServerRpc(new Vector2Int(x, y), GameManager.Instance.GetIDFromWorldObject(obj), _biomeType, dir);
    }

    public bool IsInBounds(int x, int y)
    {
        int width = MostFrontRenderedTileMatrix.GetLength(0);
        int height = MostFrontRenderedTileMatrix.GetLength(1);
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}