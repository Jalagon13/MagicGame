using UnityEngine;

public class ForestStairsGenerationStep : GenerationStep
{
    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;
        
        Debug.Log($"Forest Stairs Generation Step");
    }
}