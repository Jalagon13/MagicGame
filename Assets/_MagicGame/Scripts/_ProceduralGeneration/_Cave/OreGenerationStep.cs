using UnityEngine;

public class OreGenerationStep : GenerationStep
{
    [field: Header("Threshold Values")]
    [field: SerializeField] public float OreGenThreshold { get; private set; } = 0.05f;

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;

        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                if(caveGenData.MostFrontRenderedTileMatrix[x, y] == GameManager.Instance.GetTileIdFromTileSO(caveGenData.StoneWallTile))
                {
                    float oreGenValue = caveGenData.OreGenNM.NoiseTexture.GetPixel(x, y).grayscale;
                    if (oreGenValue > OreGenThreshold)
                    {
                        caveGenData.SetTileData(x, y, caveGenData.CoboltOreTile);
                    }
                }
            }
        }
    }
}