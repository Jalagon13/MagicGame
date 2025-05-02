using System.Collections.Generic;
using UnityEngine;

public class StoneGenerationStep : GenerationStep
{
    [field: SerializeField] public NoiseMapSO ForestStoneNoiseMap { get; private set; }
    [field: SerializeField] public NoiseMapSO RiverNoiseMap { get; private set; }

    [field: Header("Terrain Threshold Values")]
    [field: SerializeField, Range(0, 1)] 
    public float StoneWallThreshold { get; private set; }
    [field: SerializeField, Range(0, 1)] 
    public float RiverLowerThreshold { get; private set; }
    [field: SerializeField, Range(0, 1)] 
    public float RiverUpperThreshold { get; private set; }

    [field: Header("Terrain Tiles")]
    [field: SerializeField] public TileSO StoneWallTile { get; private set; }
    [field: SerializeField] public TileSO WaterTile { get; private set; }
    [field: SerializeField] public TileSO SandTile { get; private set; }
    
    private List<TileSO> _tileToCheckFor;

    public override void Execute(ForestGenerationData genData)
    {
        _tileToCheckFor = new List<TileSO>() { WaterTile, SandTile };


        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                TileSO tileSO = GameManager.Instance.GetTileSOFromID(genData.TileMatrix[x, y]);
            
                if(_tileToCheckFor.Contains(tileSO))
                {
                    continue;
                }
                
                float noiseValue = ForestStoneNoiseMap.NoiseTexture.GetPixel(x, y).grayscale;
                float riverNoise = RiverNoiseMap.NoiseTexture.GetPixel(x, y).grayscale;

                if (noiseValue > StoneWallThreshold && (riverNoise > RiverUpperThreshold || riverNoise < RiverLowerThreshold))
                {
                    genData.SetTileData(x, y, StoneWallTile);
                }
            }
        }
    }
}
