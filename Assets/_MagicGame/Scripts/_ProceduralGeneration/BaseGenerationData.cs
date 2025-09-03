using UnityEngine;

public abstract class BaseGenerationData : MonoBehaviour
{

    [HideInInspector] public ushort[,] MostFrontRenderedTileMatrix = new ushort[ChunkManager.BIOME_SIDE_LENGTH, ChunkManager.BIOME_SIDE_LENGTH];
    [HideInInspector] public string MapGenerationSeed;
    
    protected abstract BiomeType _biomeType { get; }
    public BiomeType Biome => _biomeType;

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
        
        Debug.Log($"Resetting data for biome: {_biomeType}, count: {ChunkManager.Instance.GetChunksFromBiome(_biomeType).Count}, chunks HashCode: {ChunkManager.Instance.GetChunksFromBiome(_biomeType).GetHashCode()}");
    }

    public virtual void SetTileData(int x, int y, TileDataSO tileData)
    {
        Vector2Int pos = new Vector2Int(x, y);
        ChunkManager.Instance.GetChunkFromAnyWorldPos(pos, _biomeType).AddTileData(pos, tileData);
        MostFrontRenderedTileMatrix[x, y] = GameDataRegistry.Instance.GetTileIdFromTileData(tileData);
    }

    public virtual void SetWorldObjectData(int x, int y, ResourceObject resource, CardinalDirection dir)
    {
        ChunkManager.Instance.AddResourceDataToChunkServerRpc(new Vector2Int(x, y), GameDataRegistry.Instance.GetResourceIdFromResourceData(resource.Data), _biomeType, dir);
    }

    public bool IsInBounds(int x, int y)
    {
        int width = MostFrontRenderedTileMatrix.GetLength(0);
        int height = MostFrontRenderedTileMatrix.GetLength(1);
        return x >= 0 && x < width && y >= 0 && y < height;
    }
}