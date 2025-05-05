using System.Collections.Generic;
using UnityEngine;

public class GrassGenerationStep : GenerationStep
{
    [field: SerializeField] public LandWaterGenerationStep LandWaterStep { get; private set; }
    [field: SerializeField] public float MinGrassDistance { get; private set; }
    [field: SerializeField] public float MaxGrassDistance { get; private set; }
    [field: SerializeField] public float TallGrassThreshold { get; private set; } = 0.75f;

    [field: Header("Terrain Tiles")]
    [field: SerializeField] public TileSO TallGrassTile { get; private set; }

    public override void Execute(ForestGenerationData genData)
    {
        HashSet<Vector2Int> tallGrassPoints = PoissonDiskSampling.GeneratePoints(LandWaterStep.RiverNoiseMap, MinGrassDistance, MaxGrassDistance);

        foreach (Vector2Int point in tallGrassPoints)
        {
            float tallGrassNoiseValue = LandWaterStep.RiverNoiseMap.NoiseTexture.GetPixel(point.x, point.y).grayscale;
            if (tallGrassNoiseValue > TallGrassThreshold) continue;

            if (genData.IsInBounds(point.x, point.y) && genData.MostFrontRenderedTileMatrix[point.x, point.y] == GameManager.Instance.GetTileIdFromTileSO(LandWaterStep.GrassTile))
            {
                genData.SetTileData(point.x, point.y, TallGrassTile);
            }
        }
    }
}
