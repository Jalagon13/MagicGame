using UnityEngine;

public class LandWaterGenerationStep : GenerationStep
{
    [field: SerializeField] public NoiseMapSO RiverNoiseMap { get; private set; }
    
    [field: Header("Terrain Threshold Values")]
    [field: SerializeField, Range(0, 1)] public float RiverLowerThreshold { get; private set; }
    [field: SerializeField, Range(0, 1)] public float RiverUpperThreshold { get; private set; }

    [field: Header("Terrain Tiles")]
    [field: SerializeField] public TileSO WaterTile { get; private set; }
    [field: SerializeField] public TileSO GrassTile { get; private set; }

    public override void Execute(ForestGenerationData genData)
    {
        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                float riverNoiseValue = RiverNoiseMap.NoiseTexture.GetPixel(x, y).grayscale;

                if (riverNoiseValue > RiverLowerThreshold && riverNoiseValue < RiverUpperThreshold)
                {
                    genData.SetTileData(x, y, WaterTile);
                }
                else
                {
                    genData.SetTileData(x, y, GrassTile);
                }
            }
        }
    }
}
