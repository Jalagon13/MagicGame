using UnityEngine;

public class MultiplayerManager : MonoBehaviour
{
	public static MultiplayerManager Instance { get; private set; }
	
	private void Awake()
	{
		Instance = this;
	}
	
	
}
