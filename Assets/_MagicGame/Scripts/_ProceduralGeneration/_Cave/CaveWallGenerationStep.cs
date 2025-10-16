using UnityEngine;


namespace ProjectTinker
{
    public class CaveWallGenerationStep : GenerationStep
{
    [field: Header("Threshold Values")]
    [field: SerializeField] 
    public float CheeseCaveThreshold { get; private set; } = 0.375f;
    
    [field: SerializeField] 
    public float SpaghettiCaveThresholdMin { get; private set; } = 0.45f;
    
    [field: SerializeField] 
    public float SpaghettiCaveThresholdMax { get; private set; } = 0.6f;

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;

        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                float cheeseCaveValue = caveGenData.CheeseCaveNM.NoiseTexture.GetPixel(x, y).grayscale;
                float spaghettiCaveValue = caveGenData.SpaghettiCaveNM.NoiseTexture.GetPixel(x, y).grayscale;

                if (spaghettiCaveValue < SpaghettiCaveThresholdMin || spaghettiCaveValue > SpaghettiCaveThresholdMax && cheeseCaveValue < CheeseCaveThreshold)
                {
                    caveGenData.SetTileData(x, y, caveGenData.StoneWallTile);
                }
            }
        }
    }
}
}