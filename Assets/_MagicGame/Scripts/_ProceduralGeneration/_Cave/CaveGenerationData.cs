using UnityEngine;

namespace ProjectWizard
{
    public class CaveGenerationData : BaseGenerationData
    {
        [field: Header("Terrain Tiles")]
        [field: SerializeField]
        public TileDataSO StoneTerrainTile { get; private set; }

        [field: SerializeField]
        public TileDataSO StoneWallTile { get; private set; }

        [field: SerializeField]
        public TileDataSO SilverOreTile { get; private set; }

        [field: Header("Resource Objects")]
        [field: SerializeField]
        public ResourceObject StairsToForest { get; private set; }

        [field: SerializeField]
        public ResourceObject StairsToCave { get; private set; }

        [field: SerializeField]
        public ResourceObject VisCrystalIgnis { get; private set; }

        [field: SerializeField]
        public ResourceObject VisCrystalAqua { get; private set; }

        [field: SerializeField]
        public ResourceObject VisCrystalTerra { get; private set; }

        [field: SerializeField]
        public ResourceObject VisCrystalAer { get; private set; }

        [field: Header("Noise Maps")]
        [field: SerializeField]
        public NoiseMapSO SpaghettiCaveNM;

        [field: SerializeField]
        public NoiseMapSO CheeseCaveNM;

        [field: SerializeField]
        public NoiseMapSO OreGenNM;

        protected override BiomeType _biomeType => BiomeType.Cave;
    }
}
