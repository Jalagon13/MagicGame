using System.Collections.Generic;
using UnityEngine;


namespace ProjectWizard
{
    public class TreeGenerationStep : GenerationStep
{
    [field: SerializeField] public float MinTreeDistance { get; private set; } = 3f;
    [field: SerializeField] public float MaxTreeDistance { get; private set; } = 8.5f;

    [field: Header("Terrain Threshold Values")]
    [field: SerializeField, Range(0, 1)] public float TreeThreshold { get; private set; } = 0.75f;

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not ForestGenerationData forestGenData) return;

        // NTFS: Bug: Extremely dense generate at higher noise values for some reason. Min and Max are not enforced at those levels
        HashSet<Vector2Int> treePoints = PoissonDiskSampling.GeneratePoints(forestGenData.ForestTreesNM, MinTreeDistance, MaxTreeDistance);
        
        foreach (Vector2Int point in treePoints)
        {
            float treeNoiseValue = forestGenData.ForestTreesNM.NoiseTexture.GetPixel(point.x, point.y).grayscale;
            if(treeNoiseValue > TreeThreshold) continue;

            if (forestGenData.IsInBounds(point.x, point.y) && forestGenData.MostFrontRenderedTileMatrix[point.x, point.y] == GameDataRegistry.Instance.GetTileIdFromTileData(forestGenData.GrassTerrainTile))
            {
                forestGenData.SetResourceobjectData(point.x, point.y, GameDataRegistry.Instance.GetResourceIdFromResourceData(forestGenData.TreeWorldObject.Data), CardinalDirection.North);
            }
        }
    }
}
}