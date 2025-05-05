using System.Collections.Generic;
using UnityEngine;

public class TreeGenerationStep : GenerationStep
{
    [field: SerializeField] public NoiseMapSO ForestTreesNM { get; private set; }
    [field: SerializeField] public float MinTreeDistance { get; private set; } = 3f;
    [field: SerializeField] public float MaxTreeDistance { get; private set; } = 8.5f;

    [field: Header("Terrain Threshold Values")]
    [field: SerializeField, Range(0, 1)] public float TreeThreshold { get; private set; } = 0.75f;

    [field: Header("Terrain Tiles")]
    [field: SerializeField] public TileSO GrassTile { get; private set; }
    [field: SerializeField] public TileSO TreeTile { get; private set; }

    public override void Execute(ForestGenerationData genData)
    {
        // NTFS: Bug: Extremely dense generate at higher noise values for some reason. Min and Max are not enforced at those levels
        HashSet<Vector2Int> treePoints = PoissonDiskSampling.GeneratePoints(ForestTreesNM, MinTreeDistance, MaxTreeDistance);
        
        foreach (Vector2Int point in treePoints)
        {
            float treeNoiseValue = ForestTreesNM.NoiseTexture.GetPixel(point.x, point.y).grayscale;
            if(treeNoiseValue > TreeThreshold) continue;

            if (genData.IsInBounds(point.x, point.y) && genData.MostFrontRenderedTileMatrix[point.x, point.y] == GameManager.Instance.GetTileIdFromTileSO(GrassTile))
            {
                genData.SetTileData(point.x, point.y, TreeTile);
            }
        }
    }

    
}
