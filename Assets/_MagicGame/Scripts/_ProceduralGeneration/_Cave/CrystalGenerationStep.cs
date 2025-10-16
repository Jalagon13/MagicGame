using UnityEngine;


namespace ProjectTinker
{
    public class CrystalGenerationStep : GenerationStep
{
    [SerializeField] private float _crystalSpawnThreshold = 0.025f;

    // NTFS: Figure out how I want to generate these crystals later

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;
        
        ushort ignisCrystalId = GameDataRegistry.Instance.GetResourceIdFromResourceData(caveGenData.VisCrystalIgnis.Data);
        ushort aquaCrystalId = GameDataRegistry.Instance.GetResourceIdFromResourceData(caveGenData.VisCrystalAqua.Data);
        ushort terraCrystalId = GameDataRegistry.Instance.GetResourceIdFromResourceData(caveGenData.VisCrystalTerra.Data);
        ushort aerCrystalId = GameDataRegistry.Instance.GetResourceIdFromResourceData(caveGenData.VisCrystalAer.Data);

        int crystalIndex = 0;

        // NTFS: Find a way to keep track of empty space in the world and randomly add them to the world
        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                // Debug.Log($"MFRT ID: {genData.MostFrontRenderedTileMatrix[x, y]}");
                
                if(genData.MostFrontRenderedTileMatrix[x, y] == 3) // TEMP THIS SUCKS, Figure out a better way to keep track of empty tiles
                {
                    if(Random.Range(0, 1f) < _crystalSpawnThreshold)
                    {
                        ushort crystalId;
                        switch (crystalIndex)
                        {
                            case 0:
                                crystalId = ignisCrystalId;
                                break;
                            case 1:
                                crystalId = aquaCrystalId;
                                break;
                            case 2:
                                crystalId = terraCrystalId;
                                break;
                            default:
                                crystalId = aerCrystalId;
                                break;
                        }

                        caveGenData.SetResourceobjectData(x, y, crystalId, CardinalDirection.North);

                        crystalIndex = (crystalIndex + 1) % 4;
                    }
                }
            }
        }
    }
}
}