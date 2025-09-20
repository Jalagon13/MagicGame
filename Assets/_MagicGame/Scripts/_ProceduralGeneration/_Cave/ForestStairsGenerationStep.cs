using System.Collections.Generic;
using UnityEngine;

public class ForestStairsGenerationStep : GenerationStep
{
    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;
        
        List<(int WorldObjectId, Vector2Int Position)> forestTransitionObjectData = SaveSystem.Instance.RetrieveBiomeTransitionWorldObjectData(BiomeType.Forest);
        
        foreach (var (transitionObjectId, forestTransitionObjectPosition) in forestTransitionObjectData)
        {
            ushort stairsId = GameDataRegistry.Instance.GetResourceIdFromResourceData(caveGenData.StairsToCave.Data);

            if (transitionObjectId == stairsId)
            {
                DeleteNeighborWallsAroundPoint(forestTransitionObjectPosition);
                caveGenData.SetResourceobjectData(forestTransitionObjectPosition.x, forestTransitionObjectPosition.y, stairsId, CardinalDirection.North);
            }
        }
    }

    private void DeleteNeighborWallsAroundPoint(Vector2Int centerPosition)
    {
        // Nested for loop to check all surrounding tiles within a 3x3 grid centered on the given position
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int neighborPosition = new(centerPosition.x + x, centerPosition.y + y);

                if(ChunkManager.Instance.IsWorldPosInBounds(neighborPosition))
                {
                    ChunkManager.Instance.RemoveTileServerRpc(TileType.Wall, neighborPosition, BiomeType.Cave, false);
                }
            }
        }
    }
}