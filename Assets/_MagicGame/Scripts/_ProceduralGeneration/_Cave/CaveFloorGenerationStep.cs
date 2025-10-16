using UnityEngine;

namespace ProjectTinker
{
    public class CaveFloorGenerationStep : GenerationStep
    {
        public override void Execute(BaseGenerationData genData)
        {
            if (genData is not CaveGenerationData caveGenData) return;

            // NTFS: For now, just set it to stone floor, but eventually I'll add lava and water and stuff here
            for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
            {
                for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
                {
                    caveGenData.SetTileData(x, y, caveGenData.StoneTerrainTile);
                }
            }
        }
    }
}