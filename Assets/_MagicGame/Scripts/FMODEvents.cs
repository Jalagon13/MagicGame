using UnityEngine;
using FMODUnity;

public class FMODEvents : MonoBehaviour
{
	public static FMODEvents Instance { get; private set; }
	
	[field: Header("Swing SFX")]
	[field: SerializeField] public EventReference MeleeSwing { get; private set; }
	
	private void Awake()
	{
		Instance = this;
	}
}
