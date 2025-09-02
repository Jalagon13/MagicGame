using System.Collections.Generic;
using UnityEngine;

public class ShoreLineGenerationStep : GenerationStep
{
    [field: SerializeField] public int Width { get; private set; }

    [field: Header("Terrain Threshold Values")]
    [field: SerializeField, Range(0, 1)]
    public float SandThreshold { get; private set; } = 0.5f;

    private List<TileDataSO> _tilesToSearchFor;

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not ForestGenerationData forestGenData) return;

        _tilesToSearchFor = new()
        {
            forestGenData.GrassTerrainTile,
            // Add more here in the future
        };

        HashSet<Vector2Int> initialShoreTiles = new();
        HashSet<Vector2Int> allInitialShoreTiles = GenerationUtils.GetEdgeTiles(_tilesToSearchFor, new List<TileDataSO>() { forestGenData.WaterTerrainTile }, DirectionsHelper.DirectionOffsets8, forestGenData.MostFrontRenderedTileMatrix);
        foreach (Vector2Int shoreTile in allInitialShoreTiles) // Need to do it like this so tilematrix gets updated
        {
            float noiseValue = forestGenData.SandNoiseMap.NoiseTexture.GetPixel(shoreTile.x, shoreTile.y).grayscale;

            if (noiseValue > SandThreshold)
            {
                forestGenData.SetTileData(shoreTile.x, shoreTile.y, forestGenData.SandTerrainTile);
                initialShoreTiles.Add(shoreTile);
            }
        }
        
        // Width - 1 because initial shore tiles are already expanded
        HashSet<Vector2Int> shoreTiles = GenerationUtils.ExpandEdgeTiles(initialShoreTiles, Width - 1, _tilesToSearchFor, forestGenData.MostFrontRenderedTileMatrix);
        foreach (Vector2Int shoreTile in shoreTiles)
        {
            forestGenData.SetTileData(shoreTile.x, shoreTile.y, forestGenData.SandTerrainTile);
        }
    }
}
