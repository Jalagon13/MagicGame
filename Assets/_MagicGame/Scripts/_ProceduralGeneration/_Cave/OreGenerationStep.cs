using UnityEngine;

public class OreGenerationStep : GenerationStep
{
    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;
        
        Debug.Log($"Ore Generation Step");
    }
}