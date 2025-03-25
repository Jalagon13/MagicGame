using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

[Flags]
public enum BiomeType // NTFS: When adding new IDs remember to put the value to the next power of 2 for the [Flags] to work properly
{
	Forest = 0,
	Cave = 1
}

public class WorldManager : NetworkBehaviour
{
	public static WorldManager Instance { get; private set; }
	
	public event EventHandler OnBiomeTransitionStart;
	public event EventHandler OnBiomeTransitionEnd;
	public event EventHandler OnBiomeDataLoaded;
	public event EventHandler<OnTickEventArgs> OnTick;
	public class OnTickEventArgs : EventArgs 
	{
		public float CurrentDayRatio;
		public float CurrentTime;
		public float DayDuration;
	}
	
	public bool IsNight { get; private set; }
	public bool IsLoadingBiome { get; private set; }

	[Title("Boundaries", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private float _dayDurationInSeconds;
	[SerializeField] private float _startingTime = 0.0f;
	[SerializeField] private bool _isTicking = true;
	
	[Title("World Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private bool _randomSeed = false;
	[SerializeField] private string _customSeed = 123.ToString();
	[SerializeField] private float _portalSearchRadius = 10f;
	[SerializeField] private float _portalSearchDelayOnBiomeLoad = 0.75f;
	[SerializeField] private float _endBiomeTransitionDelay = 1f;
	
	private float _currentTime;
	
	public string Seed 
	{ 
		get 
		{ 
			return _randomSeed ? Time.time.ToString() : _customSeed; 
		} 
	}
	
	private void Awake()
	{
		Instance = this;
	}
	
	private IEnumerator Start()
	{
		if(IsServer)
		{
			NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected_SyncTime;
		}
	
		_currentTime = _startingTime;
		
		// We need to ensure that we don't have a day length at 0, otherwise we will get stuck into infinite loop in update.
		if (_dayDurationInSeconds <= 0.0f)
		{
			// (and a day with 0 length makes no sense)
			_dayDurationInSeconds = 1.0f;
		}
		
		yield return new WaitForEndOfFrame();
		
		Tick(); // Tick once to trigger initial light update
	}

	private void Update()
	{
		if(_isTicking)
			Tick();
	}
	
	private void Tick()
	{
		_currentTime += Time.deltaTime;

		while (_currentTime > _dayDurationInSeconds)
		{
			_currentTime -= _dayDurationInSeconds;
		
			// Resync time for all clients
			if (IsServer)
			{
				foreach (var clientId in NetworkManager.ConnectedClientsIds)
				{
					SyncTimeForClientRpc(_currentTime, _dayDurationInSeconds, RpcTarget.Single(clientId, RpcTargetUse.Persistent));
				}
			}
		}

		float currentDayRatio = _currentTime / _dayDurationInSeconds;

		IsNight = currentDayRatio >= 0.5f && currentDayRatio < 1f; // Set IsNight to true if the current ratio is between 0.5 (halfway) and 1 (end of the day)

		OnTick?.Invoke(this, new OnTickEventArgs
		{
			CurrentDayRatio = currentDayRatio,
			CurrentTime = _currentTime,
			DayDuration = _dayDurationInSeconds
		});
	}
	
	private void OnClientConnected_SyncTime(ulong clientId)
	{
		SyncTimeForClientRpc(_currentTime, _dayDurationInSeconds, RpcTarget.Single(clientId, RpcTargetUse.Persistent));
	}

	[Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
	private void SyncTimeForClientRpc(float currentTime, float dayDurationInSeconds, RpcParams rpcParams)
	{
		_currentTime = currentTime;
		_dayDurationInSeconds = dayDurationInSeconds;
	}

	public void LoadBiome(BiomeType targetBiome, Vector2 position)
	{
		PlacePlayerAt(position);
		
		OnBiomeTransitionStart?.Invoke(this, EventArgs.Empty); 
		
		IsLoadingBiome = true;
		ChunkManager.Instance.UnloadAllChunks();
		ObjectManager.Instance.ClearAllEnvironmentObjectVisuals();
		
		LoadEnvironmentServerRpc(Player.LocalClientInstance.CurrentPlayerBiome.Value, targetBiome);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void LoadEnvironmentServerRpc(BiomeType fromBiome, BiomeType toBiome, RpcParams rpcParams = default)
	{
		AsyncLoadEnvironment(fromBiome, toBiome, rpcParams);
	}

	private async void AsyncLoadEnvironment(BiomeType fromBiome, BiomeType toBiome, RpcParams rpcParams = default)
	{
		// Save the last biome it came from and set the player's burrent biome to tobiome.
		if(!SaveSystem.Instance.IsSaving && SaveSystem.Instance.BiomeLoadedInMemory(fromBiome))
		{
			await SaveSystem.Instance.SaveBiome(fromBiome);
		}
		
		// If the targetBiome is already loaded into memory, 
		if(SaveSystem.Instance.BiomesInMemory.Contains(toBiome))
		{
			LoadChunksClientRpc(toBiome, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
		}
		else
		{
			// If not, deserializeanddispatch data
			if(SaveSystem.Instance.BiomeSaveFileExists(toBiome))
			{
				await SaveSystem.Instance.DeserializeAndDispatchData(toBiome);
			}
			else
			{
				GenerateBiome(toBiome);
				
				await SaveSystem.Instance.SaveBiome(fromBiome);
			}
			
			LoadChunksClientRpc(toBiome, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
		}
	}

	private void GenerateBiome(BiomeType toBiome)
	{
		switch (toBiome)
		{
			case BiomeType.Forest:
				GetComponent<ForestGeneration>().GenerateForest();
				break;
			case BiomeType.Cave:
				GetComponent<CaveGeneration>().GenerateCave();
				break;
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void LoadChunksClientRpc(BiomeType toBiome, RpcParams rpcParams)
	{
		Debug.Log($"Player biome: {toBiome} Allowing client to load chunks");
		Player.LocalClientInstance.CurrentPlayerBiome.Value = toBiome;
		
		// Invoke it first to prep the last chunk position to garentee a new set of chunks to generate, then set loadingbiome to true to resume the update method
		TileManager.Instance.TileVisibilityDict.Clear();
		OnBiomeDataLoaded?.Invoke(this, EventArgs.Empty);
		IsLoadingBiome = false;
		
		StartCoroutine(SearchForPortal());
	}

	private IEnumerator SearchForPortal()
	{
		yield return new WaitForSeconds(_endBiomeTransitionDelay);

		Lightmap.Instance.UpdateLightMap();
		
		OnBiomeTransitionEnd?.Invoke(this, EventArgs.Empty); 
	}

	private void PlacePlayerAt(Vector2 portalPosition)
	{
		Player.LocalClientInstance.transform.SetPositionAndRotation(new(portalPosition.x + 0.5f, portalPosition.y + 0.5f), Quaternion.identity);
	}
	
	
	public bool IsTicking()
	{
		return _isTicking;
	}
	
	public override void OnDestroy()
	{
		if(IsServer)
		{
			if(NetworkManager.Singleton != null)
			{
				NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected_SyncTime;
			}
		}
		
		base.OnDestroy();
	}
}
