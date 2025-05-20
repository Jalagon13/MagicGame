using System.Collections.Generic;
using UnityEngine;

public class TallGrassGenerationStep : GenerationStep
{
    [field: SerializeField] public float MinGrassDistance { get; private set; }
    [field: SerializeField] public float MaxGrassDistance { get; private set; }
    [field: SerializeField] public float TallGrassThreshold { get; private set; } = 0.75f;

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not ForestGenerationData forestGenData) return;

        HashSet<Vector2Int> tallGrassPoints = PoissonDiskSampling.GeneratePoints(forestGenData.ForestTallGrassNM, MinGrassDistance, MaxGrassDistance);

        foreach (Vector2Int point in tallGrassPoints)
        {
            float tallGrassNoiseValue = forestGenData.ForestTallGrassNM.NoiseTexture.GetPixel(point.x, point.y).grayscale;
            if (tallGrassNoiseValue > TallGrassThreshold) continue;

            if (forestGenData.IsInBounds(point.x, point.y) && forestGenData.MostFrontRenderedTileMatrix[point.x, point.y] == GameManager.Instance.GetTileIdFromTileSO(forestGenData.GrassTerrainTile))
            {
                forestGenData.SetTileData(point.x, point.y, forestGenData.TallGrassTile);
            }
        }
    }
}
