using UnityEngine;


namespace ProjectTinker
{
	public abstract class GenerationStep : MonoBehaviour
	{
	    [field: SerializeField, TextArea(15, 20)] public string Description { get; private set; }
    
	    public abstract void Execute(BaseGenerationData genData);
	}

}