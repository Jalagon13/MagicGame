using UnityEngine;

public class CrystalGenerationStep : GenerationStep
{
    

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;
        
        // NTFS: Find a way to keep track of empty space in the world and randomly add them to the world
    }
}
