using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using Unity.Netcode;
using System;

public enum Ambience 
{
    ForestAmbience = 0,
	CaveAmbience = 1
}

public class SoundManager : NetworkBehaviour
{
	public static SoundManager Instance { get; private set; }
	
	private EventInstance _ambienceEventInstance;

	private void Awake()
	{
		Instance = this;

		if (NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback += InitializeSounds;
		}
	}

    private void Start()
    {
		if(GameWorld.Instance != null)
		{
			GameWorld.Instance.OnBiomeDataLoaded += ChangeAmbience;
		}
	}

    private void ChangeAmbience(object sender, EventArgs e)
    {
        if (Player.Instance != null)
        {
            Ambience ambience;

            switch (Player.Instance.CurrentBiome.Value)
            {
                case BiomeType.Forest:
                    ambience = Ambience.ForestAmbience;
                    break;
                case BiomeType.Cave:
                    ambience = Ambience.CaveAmbience;
                    break;
                default:
					_ambienceEventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
					return; // Exit if the biome doesn't match any known types
            }

            SetAmbience(ambience);
        }
    }

    private void InitializeSounds(ulong obj)
    {
        InitializeAmbience(FMODEvents.Instance.Ambience);
    }
    
    public void SetAmbience(Ambience ambience)
    {
        _ambienceEventInstance.setParameterByName("Ambience", (float)ambience);
    }

    public void InitializeAmbience(EventReference ambienceEventReference)
	{
		_ambienceEventInstance = CreateInstance(ambienceEventReference);
		_ambienceEventInstance.start();
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

	public override void OnDestroy()
	{
		if (NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback -= InitializeSounds;
		}

		if (GameWorld.Instance != null)
		{
			GameWorld.Instance.OnBiomeDataLoaded -= ChangeAmbience;
		}

		base.OnDestroy();
	}

    internal void PlayOneShot(object value, Vector3 position)
    {
        throw new NotImplementedException();
    }
}