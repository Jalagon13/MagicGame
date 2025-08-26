using System;
using UnityEngine;

// Facilitates passing generation data to forest chunks
public class ForestGenerationData : BaseGenerationData
{
    [field: Header("Terrain Tiles")]
    [field: SerializeField] public TileSO GrassTerrainTile { get; private set; }
    [field: SerializeField] public TileSO SandTerrainTile { get; private set; }
    [field: SerializeField] public TileSO WaterTerrainTile { get; private set; }
    [field: SerializeField] public TileSO TallGrassTile { get; private set; }

    [field: Header("World Objects")]
    [field: SerializeField] public ResourceObject TreeWorldObject { get; private set; }
    [field: SerializeField] public ResourceObject CaveEntranceWorldObject { get; private set; }

    [field: Header("Noise Maps")]
    [field: SerializeField] public NoiseMapSO RiverNoiseMap { get; private set; }
    [field: SerializeField] public NoiseMapSO SandNoiseMap { get; private set; }
    [field: SerializeField] public NoiseMapSO ForestTreesNM { get; private set; }
    [field: SerializeField] public NoiseMapSO ForestTallGrassNM { get; private set; }

    protected override BiomeType _biomeType => BiomeType.Forest;
}
