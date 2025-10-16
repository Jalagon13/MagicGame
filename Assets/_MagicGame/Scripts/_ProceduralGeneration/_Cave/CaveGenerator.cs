using System.Collections.Generic;
using UnityEngine;


namespace ProjectTinker
{
    public class CaveGenerator : MonoBehaviour
{
    [field: SerializeField] 
    public CaveGenerationData CaveGenerationData { get; private set; }
    
    [field: SerializeField] 
    public GenerationStep[] GenerationSteps { get; private set; }
    
    [field: SerializeField] 
    public List<NoiseMapSO> NoiseMapsToApplySeed { get; private set; }

    public void GenerateCave()
    {
        ChunkManager.IS_GENERATING_BIOME = true;

        // Generate seed and noise textures 
        CaveGenerationData.MapGenerationSeed = GameWorld.Instance.Seed;
        foreach (NoiseMapSO noiseMapSO in NoiseMapsToApplySeed)
        {
            noiseMapSO.GenerateNoiseTexture(CaveGenerationData.MapGenerationSeed);
        }

        // Reset chunk data
        CaveGenerationData.ResetData();

        // Run generation steps
        foreach (GenerationStep generationStep in GenerationSteps)
        {
            generationStep?.Execute(CaveGenerationData);
        }

        // Various end of generation tasks
        SaveSystem.Instance.AddBiomeToMemorySessionTracker(BiomeType.Cave);
        
        ChunkManager.IS_GENERATING_BIOME = false;
    }
}
}