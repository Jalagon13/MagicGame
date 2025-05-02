using System.Collections.Generic;
using UnityEngine;

public class ExpandShoreLineGenerationStep : GenerationStep
{
    [field: SerializeField] public NoiseMapSO SandNoiseMap { get; private set; }
    [field: SerializeField] public int Width { get; private set; }

    [field: Header("Terrain Threshold Values")]
    [field: SerializeField, Range(0, 1)]
    public float SandThreshold { get; private set; } = 0.5f;

    [field: Header("Terrain Tiles")]
    [field: SerializeField] public TileSO SandTile { get; private set; }
    [field: SerializeField] public TileSO GrassTile { get; private set; }
    [field: SerializeField] public TileSO WaterTile { get; private set; }

    private List<TileSO> _tilesToSearchFor;

    public override void Execute(ForestGenerationData genData)
    {
        _tilesToSearchFor = new()
        {
            GrassTile,
            // Add more here in the future
        };

        HashSet<Vector2Int> initialShoreTiles = new();
        HashSet<Vector2Int> allInitialShoreTiles = GenerationUtils.GetEdgeTiles(_tilesToSearchFor, new List<TileSO>() { WaterTile }, DirectionsHelper.DirectionOffsets8, genData.TileMatrix);
        foreach (Vector2Int shoreTile in allInitialShoreTiles) // Need to do it like this so tilematrix gets updated
        {
            float noiseValue = SandNoiseMap.NoiseTexture.GetPixel(shoreTile.x, shoreTile.y).grayscale;

            if (noiseValue > SandThreshold)
            {
                genData.SetTileData(shoreTile.x, shoreTile.y, SandTile);
                initialShoreTiles.Add(shoreTile);
            }
        }
        
        // Width - 1 because initial shore tiles are already expanded
        HashSet<Vector2Int> shoreTiles = GenerationUtils.ExpandEdgeTiles(initialShoreTiles, Width - 1, _tilesToSearchFor, genData.TileMatrix);
        foreach (Vector2Int shoreTile in shoreTiles)
        {
            genData.SetTileData(shoreTile.x, shoreTile.y, SandTile);
        }
    }
}
