using UnityEngine;


namespace ProjectTinker
{
    public class LandWaterGenerationStep : GenerationStep
{
    [field: Header("Terrain Threshold Values")]
    [field: SerializeField, Range(0, 1)] public float RiverLowerThreshold { get; private set; }
    [field: SerializeField, Range(0, 1)] public float RiverUpperThreshold { get; private set; }

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not ForestGenerationData forestGenData) return;

        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                float riverNoiseValue = forestGenData.RiverNoiseMap.NoiseTexture.GetPixel(x, y).grayscale;

                if (riverNoiseValue > RiverLowerThreshold && riverNoiseValue < RiverUpperThreshold)
                {
                    forestGenData.SetTileData(x, y, forestGenData.SandTerrainTile);
                    forestGenData.SetTileData(x, y, forestGenData.WaterTerrainTile);
                }
                else
                {
                    forestGenData.SetTileData(x, y, forestGenData.GrassTerrainTile);
                }
            }
        }
    }
}
}