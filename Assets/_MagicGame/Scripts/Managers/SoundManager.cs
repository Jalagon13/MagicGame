using UnityEngine;
using FMODUnity;

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance { get; private set; }
	
	private void Awake()
	{
		Instance = this;
	}
	
	public void PlayOneShot(EventReference sound, Vector3 worldPos)
	{
		RuntimeManager.PlayOneShot(sound, worldPos);
	}
}
