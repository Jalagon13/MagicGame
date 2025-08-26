using UnityEngine;

public class CaveGenerationData : BaseGenerationData
{
    [field: Header("Terrain Tiles")]
    [field: SerializeField] public TileSO StoneTerrainTile { get; private set; }
    [field: SerializeField] public TileSO StoneWallTile { get; private set; }
    [field: SerializeField] public TileSO CoboltOreTile { get; private set; }

    [field: Header("World Objects")]
    [field: SerializeField] public ResourceObject StairsToForest { get; private set; }
    [field: SerializeField] public ResourceObject StairsToCave { get; private set; }

    [field: Header("Noise Maps")]
    [field: SerializeField] public NoiseMapSO SpaghettiCaveNM;
    [field: SerializeField] public NoiseMapSO CheeseCaveNM;
    [field: SerializeField] public NoiseMapSO OreGenNM;

    protected override BiomeType _biomeType => BiomeType.Cave;
}
