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
	
	public event EventHandler OnStartBiomeTransition;
	public event EventHandler OnEndBiomeTransition;
	public event EventHandler OnBiomeDataLoaded;
	public event EventHandler<OnTickEventArgs> OnTick;
	public class OnTickEventArgs : EventArgs 
	{
		public float CurrentDayRatio;
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
	[SerializeField] private WorldObject _portalObjectPrefab;
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
		
		// Tick once to trigger initial light update
		Tick();
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

		// Calculate the ratio of the day
		float currentDayRatio = _currentTime / _dayDurationInSeconds;

		// Set IsNight to true if the current ratio is between 0.5 (halfway) and 1 (end of the day)
		IsNight = currentDayRatio >= 0.5f && currentDayRatio < 1f;

		OnTick?.Invoke(this, new OnTickEventArgs
		{
			CurrentDayRatio = currentDayRatio
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

	public void LoadBiome(BiomeType targetBiome, Vector2 portalPosition, bool searchForPortal = true)
	{
		PlacePlayerAt(portalPosition);
		
		OnStartBiomeTransition?.Invoke(this, EventArgs.Empty); 
		
		IsLoadingBiome = true;
		ChunkManager.Instance.UnloadAllChunks();
		ObjectManager.Instance.ClearAllEnvironmentObjectVisuals();
		Debug.Log($"a");
		LoadEnvironmentServerRpc(searchForPortal, portalPosition, Player.LocalClientInstance.CurrentBiome.Value, targetBiome);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void LoadEnvironmentServerRpc(bool searchForPortal, Vector2 portalPosition, BiomeType fromBiome, BiomeType toBiome, RpcParams rpcParams = default)
	{
		Debug.Log("b");
		AsyncLoadEnvironment(searchForPortal, portalPosition, fromBiome, toBiome, rpcParams);
	}

	private async void AsyncLoadEnvironment(bool searchForPortal, Vector2 portalPosition, BiomeType fromBiome, BiomeType toBiome, RpcParams rpcParams = default)
	{
		// Save the last biome it came from and set the player's burrent biome to tobiome.
		if(!SaveSystem.Instance.IsSaving && SaveSystem.Instance.BiomeLoadedInMemory(fromBiome))
		{
			Debug.Log($"Saving biome because is not saving and biome is loaded in memeory");
			await SaveSystem.Instance.SaveBiome(fromBiome);
		}
		Debug.Log($"c, sender client id: {rpcParams.Receive.SenderClientId}");
		
		// If the targetBiome is already loaded into memory, 
		if(SaveSystem.Instance.BiomesInMemory.Contains(toBiome))
		{
			// Load chunks around this player
			Debug.Log($"Biome in memory already");
			LoadChunksClientRpc(toBiome, searchForPortal, portalPosition, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
		}
		else
		{
			// If not, deserializeanddispatch data
			Debug.Log($"biome not in memory, need to deserialize and dispatch data if the save file exists or generate the new biome");
			
			if(SaveSystem.Instance.BiomeSaveFileExists(toBiome))
			{
				Debug.Log($"Unloading biome data and dispatching it");
				await SaveSystem.Instance.DeserializeAndDispatchData(toBiome);
			}
			else
			{
				GenerateBiome(toBiome);
				
				Debug.Log($"Biome generated and saving it");
				await SaveSystem.Instance.SaveBiome(fromBiome);
			}
			
			LoadChunksClientRpc(toBiome, searchForPortal, portalPosition, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
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
	private void LoadChunksClientRpc(BiomeType toBiome, bool searchForPortal, Vector2 portalPosition, RpcParams rpcParams)
	{
		Debug.Log($"Player biome: {toBiome}Allowing client to load chunks");
		Player.LocalClientInstance.CurrentBiome.Value = toBiome;
		
		// Invoke it first to prep the last chunk position to garentee a new set of chunks to generate, then set loadingbiome to true to resume the update method
		OnBiomeDataLoaded?.Invoke(this, EventArgs.Empty);
		IsLoadingBiome = false;
		
		StartCoroutine(SearchForPortal(searchForPortal, portalPosition));
		
		
	}
	
	

	private IEnumerator SearchForPortal(bool searchForPortal, Vector2 portalPosition)
	{
		if(searchForPortal)
		{
			yield return new WaitForSeconds(_portalSearchDelayOnBiomeLoad);

			Collider2D[] colliders = Physics2D.OverlapCircleAll(portalPosition, _portalSearchRadius);

			Portal closestPortal = null;
			float closestDistance = float.MaxValue;

			// Loop through the colliders to find portals and the closest one
			foreach (var collider in colliders)
			{
				Portal portal = collider.GetComponent<Portal>();
				if (portal != null)
				{
					float distance = Vector2.Distance(portal.transform.position, portalPosition);

					if (distance < closestDistance)
					{
						closestPortal = portal;
						closestDistance = distance;
					}
				}
			}

			if (closestPortal != null)
			{
				Debug.Log($"Closest portal found at: {closestPortal.transform.position}");
				PlacePlayerAt(closestPortal.transform.position);
			}
			else
			{
				Debug.Log("No portal found within the search radius.");
				SpawnPortal(portalPosition);
			}
		}
		
		yield return new WaitForSeconds(_endBiomeTransitionDelay);
		
		OnEndBiomeTransition?.Invoke(this, EventArgs.Empty); 
	}

	private void SpawnPortal(Vector2 portalPosition)
	{
		Debug.Log("Portal NOT found. Placing player at new portal that is spawned");
		Vector2Int v2IntPos = new(Mathf.RoundToInt(portalPosition.x), Mathf.RoundToInt(portalPosition.y));
		ObjectManager.Instance.PlaceObject(v2IntPos, _portalObjectPrefab, Player.LocalClientInstance.CurrentBiome.Value);
	}
	
	private void PlacePlayerAt(Vector2 portalPosition)
	{
		Player.LocalClientInstance.transform.SetPositionAndRotation(new(portalPosition.x + 0.5f, portalPosition.y - 0.5f), Quaternion.identity);
	}
	
	private void DeleteNeighborWallsAroundPoint(Vector3 centerPosition)
	{
		Vector3Int centerPositionInt = Vector3Int.FloorToInt(centerPosition);
	
		// Nested for loop to check all surrounding tiles within a 3x3 grid centered on the given position
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				Vector3Int neighborPosition = new(centerPositionInt.x + x, centerPositionInt.y + y, centerPositionInt.z);
				Environment.Instance.GetWallTilemapData().DeleteTile(new(neighborPosition.x, neighborPosition.y), Player.LocalClientInstance.CurrentBiome.Value);
			}
		}
	}
	
	public bool IsTicking()
	{
		return _isTicking;
	}
	
	public override void OnDestroy()
	{
		if(IsServer)
		{
			NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected_SyncTime;
		}
		
		base.OnDestroy();
	}
}
