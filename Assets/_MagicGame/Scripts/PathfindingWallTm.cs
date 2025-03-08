using UnityEngine;

public class PathfindingWallTm : MonoBehaviour
{
    public BiomeType Biome { get; private set; }
    
    public void SetBiome(BiomeType biome)
    {
        Biome = biome;
    }
    
    public bool BiomeSameAs(BiomeType biome)
    {
        return Biome == biome;
    }
}
