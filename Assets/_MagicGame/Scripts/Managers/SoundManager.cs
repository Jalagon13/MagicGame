using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class SoundManager : MonoBehaviour
{
	public static SoundManager Instance { get; private set; }

	private void Awake()
	{
		Instance = this;
	}

	// Play a sound one time at a specific world position
	public void PlayOneShot(EventReference sound, Vector3 worldPos)
	{
		RuntimeManager.PlayOneShot(sound, worldPos);
	}

	// Create an event instance
	public EventInstance CreateInstance(EventReference eventReference)
	{
		EventInstance eventInstance = RuntimeManager.CreateInstance(eventReference);
		return eventInstance;
	}
}