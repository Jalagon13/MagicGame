using UnityEngine;

public class CrystalGenerationStep : GenerationStep
{

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;
        
        ushort crystalId = GameDataRegistry.Instance.GetResourceIdFromResourceData(caveGenData.VisCrystalIgnis.Data);

        // NTFS: Find a way to keep track of empty space in the world and randomly add them to the world
        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                // Debug.Log($"MFRT ID: {genData.MostFrontRenderedTileMatrix[x, y]}");
                
                if(genData.MostFrontRenderedTileMatrix[x, y] == 3) // TEMP THIS SUCKS, Figure out a better way to keep track of empty tiles
                {
                    if(Random.Range(0, 1f) < 0.15f)
                    {
                        caveGenData.SetResourceobjectData(x, y, crystalId, CardinalDirection.North);
                    }
                }
            }
        }
    }
}
