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
	
	public event EventHandler OnBiomeLoaded;
	public event EventHandler<OnTickEventArgs> OnTick;
	public class OnTickEventArgs : EventArgs 
	{
		public float CurrentDayRatio;
	}
	
	public bool IsNight { get; private set; }
	public bool IsLoadingBiome { get; private set; }

	[Title("Bounaries", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private float _dayDurationInSeconds;
	[SerializeField] private float _startingTime = 0.0f;
	[SerializeField] private bool _isTicking = true;
	
	[Title("World Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private bool _randomSeed = false;
	[SerializeField] private string _customSeed = 123.ToString();
	[SerializeField] private WorldObject _portalObjectPrefab;
	
	private List<BiomeType> _environmentList = new(); // Used to keep track which environments have been generated or not
	private float _currentTime;
	private bool _isTransitioningEnvironment, _isPlayerRespawning;
	
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

	public void GenerateEnvironment(BiomeType biomeToGenerate)
	{
		// Check if environment is already generated
		if(_environmentList.Count > 0)
		{
			foreach (BiomeType environment in _environmentList)
			{
				if(environment == biomeToGenerate)
				{
					// Environment found, should not generate an environment already found
					Debug.LogError("Should not be trying to generate an environment that is already generated");
					return;
				}
			}
		}
		
		LoadEnvironment(biomeToGenerate, Player.LocalClientInstance.transform.position);
	}
	
	public void LoadEnvironment(BiomeType targetBiome, Vector2 portalPosition, bool isPlayerRespawning = false)
	{
		_isPlayerRespawning = isPlayerRespawning;
		IsLoadingBiome = true;
		
		// NTFS: make sure player is not able to move during this process and add a loading screen
		
		// Teleport player to portal he is entering
		PlacePlayerAt(portalPosition);
		
		// Clear all client visuals
		ChunkManager.Instance.UnloadAllChunks();
		ObjectManager.Instance.ClearAllEnvironmentObjectVisuals();
		// ChunkManager.Instance.OnLoadedPlayerChunksUpdated += OnClientEnvironmentTransitionEnd;
		Debug.Log("a");
		LoadEnvironmentServerRpc(Player.LocalClientInstance.CurrentBiome.Value, targetBiome);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void LoadEnvironmentServerRpc(BiomeType fromBiome, BiomeType toBiome)
	{
		Debug.Log("b");
		AsyncLoadEnvironment(fromBiome, toBiome);
	}

	private async void AsyncLoadEnvironment(BiomeType fromBiome, BiomeType toBiome, RpcParams rpcParams = default)
	{
		// Save the last biome it came from and set the player's burrent biome to tobiome.
		if(!SaveSystem.Instance.IsSaving && SaveSystem.Instance.BiomeLoadedInMemory(fromBiome))
		{
			Debug.Log($"Saving biome because is not saving and biome is loaded in memeory");
			await SaveSystem.Instance.SaveBiome(fromBiome);
		}
		Debug.Log("c");
		NetworkManager.Singleton.ConnectedClients[rpcParams.Receive.SenderClientId].PlayerObject.GetComponent<Player>().CurrentBiome.Value = toBiome;
		
		// If the targetBiome is already loaded into memory, 
		if(SaveSystem.Instance.BiomesInMemory.Contains(toBiome))
		{
			// Load chunks around this player
			Debug.Log($"Biome in memory already");
			LoadChunksClientRpc(RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
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
			
			LoadChunksClientRpc(RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
		}
	}

	private void GenerateBiome(BiomeType toBiome)
	{
		switch (toBiome)
		{
			case BiomeType.Forest:
				_environmentList.Add(BiomeType.Forest);
				GetComponent<ForestGeneration>().GenerateForest();
				break;
			case BiomeType.Cave:
				_environmentList.Add(BiomeType.Cave);
				GetComponent<CaveGeneration>().GenerateCave();
				break;
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void LoadChunksClientRpc(RpcParams rpcParams)
	{
		Debug.Log($"Allowing client to load chunks");
		// Invoke it first to prep the last chunk position to garentee a new set of chunks to generate, then set loadingbiome to true to resume the update method
		OnBiomeLoaded?.Invoke(this, EventArgs.Empty);
		IsLoadingBiome = false;
	}
	
	private void OnClientEnvironmentTransitionEnd(object sender, ChunkManager.OnActiveChunksUpdatedEventArgs e)
	{
		// ChunkManager.Instance.OnLoadedPlayerChunksUpdated -= OnClientEnvironmentTransitionEnd;
		
		SearchForPortal(Player.LocalClientInstance.transform.position);
	}

	private void SearchForPortal(Vector2 portalPosition)
	{
		// If player is respawning after loading this environment, do not search for portal
		if(_isPlayerRespawning)
		{
			_isPlayerRespawning = false;
			return;
		}
		
		_isPlayerRespawning = false;

		// If there is a portal in lets say a 10 tile radius, grab it's position, and teleport player to that portal
		float searchRadius = 10f;

		// Find all colliders in the circular search area
		Collider2D[] colliders = Physics2D.OverlapCircleAll(portalPosition, searchRadius);

		// Initialize variables to track the closest portal
		Portal closestPortal = null;
		float closestDistance = float.MaxValue;

		// Loop through the colliders to find portals and the closest one
		foreach (var collider in colliders)
		{
			// Check if the collider has a Portal component
			Portal portal = collider.GetComponent<Portal>();
			if (portal != null)
			{
				// Calculate the distance from the portal to the given portal position
				float distance = Vector2.Distance(portal.transform.position, portalPosition);

				// Update the closest portal if this one is closer
				if (distance < closestDistance)
				{
					closestPortal = portal;
					closestDistance = distance;
				}
			}
		}

		// If a closest portal was found, do something with it
		if (closestPortal != null)
		{
			// Example: Log the closest portal's position (you can replace this with your desired logic)
			Debug.Log($"Closest portal found at: {closestPortal.transform.position}");
			PlacePlayerAt(closestPortal.transform.position);
			// Your custom logic here (e.g., teleport the player to this portal)
		}
		else
		{
			Debug.Log("No portal found within the search radius.");
			SpawnPortal(portalPosition);
		}
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
	
	public bool GetIsTransitioningEnvironment()
	{
		return _isTransitioningEnvironment;
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
