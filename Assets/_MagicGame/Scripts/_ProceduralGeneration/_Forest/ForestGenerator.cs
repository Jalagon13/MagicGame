using System.Collections.Generic;
using UnityEngine;

// Facilitates broad step by step process to generate forest
public class ForestGenerator : MonoBehaviour
{
    [field: SerializeField] public ForestGenerationData ForestGenerationData { get; private set; }
    [field: SerializeField] public GenerationStep[] GenerationSteps { get; private set; }
    [field: SerializeField] public List<NoiseMapSO> NoiseMapsToApplySeed { get; private set; }

    public void GenerateForest()
    {
        ChunkManager.IS_GENERATING_BIOME = true;

        // Generate seed and noise textures 
        ForestGenerationData.MapGenerationSeed = WorldManager.Instance.Seed;
        foreach (NoiseMapSO noiseMapSO in NoiseMapsToApplySeed)
        {
            noiseMapSO.GenerateNoiseTexture(ForestGenerationData.MapGenerationSeed);
        }
        
        // Reset chunk data
        ForestGenerationData.ResetData();
        
        // Run generation steps
        foreach (GenerationStep generationStep in GenerationSteps)
        {
            generationStep?.Execute(ForestGenerationData);
        }

        // Various end of generation tasks
        SaveSystem.Instance.AddBiomeToMemorySessionTracker(BiomeType.Forest);
        ChunkManager.IS_GENERATING_BIOME = false;
    }
}
