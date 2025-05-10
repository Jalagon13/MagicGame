using System.Collections.Generic;
using UnityEngine;

public class CaveEntranceGenerationStep : GenerationStep
{
    [field: SerializeField] public float MinCaveEntranceDistance { get; private set; } = 20f;
    [field: SerializeField] public float MaxCaveEntranceDistance { get; private set; } = 40f;
    [field: SerializeField] public float CaveEntranceSpawnThreshold { get; private set; } = 0.85f;

    public override void Execute(BaseGenerationData genData)
    {
        if(genData is not ForestGenerationData forestGenData) return;
    
        HashSet<Vector2Int> entrancePoints = PoissonDiskSampling.GeneratePoints(forestGenData.ForestTallGrassNM, MinCaveEntranceDistance, MaxCaveEntranceDistance);

        foreach (Vector2Int point in entrancePoints)
        {
            float entranceNoiseValue = forestGenData.ForestTallGrassNM.NoiseTexture.GetPixel(point.x, point.y).grayscale;
            if (entranceNoiseValue > CaveEntranceSpawnThreshold) continue;

            if (forestGenData.IsInBounds(point.x, point.y) && forestGenData.MostFrontRenderedTileMatrix[point.x, point.y] == GameManager.Instance.GetTileIdFromTileSO(forestGenData.GrassTerrainTile))
            {
                Debug.Log($"Cave Entrance at {point}");
                forestGenData.SetWorldObjectData(point.x, point.y, forestGenData.CaveEntranceWorldObject, CardinalDirection.North);
            }
        }
    }
}
