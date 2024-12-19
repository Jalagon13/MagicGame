using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldManager : NetworkBehaviour
{
	[Flags]
	public enum EnvironmentID // NTFS: when adding new IDs remember to put the value to the next power of 2
	{
		Forest = 0,
		Cave = 1
	}
	
	public static WorldManager Instance { get; private set; }
	private static EnvironmentID ACTIVE_ENVIRONMENT_ID;

	[Title("Bounaries", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private float _dayDurationInSeconds;
	[SerializeField] private float _startingTime = 0.0f;
	[SerializeField] private bool _isTicking = true;
	
	[Title("World Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private bool _randomSeed = false;
	[SerializeField] private string _customSeed = 123.ToString();
	[SerializeField] private WorldObject _portalObjectPrefab;
	
	// Class globals
	private List<EnvironmentID> _environmentList = new(); // Used to keep track which environments have been generated or not
	private float _currentTime;
	private bool _isTransitioningEnvironment;
	
	// public float CurrentDayRatio => _currentTime / _dayDurationInSeconds;
	// public DayCycleHandler DayCycleHandler { get; set; }
	// public LightMap LightMap { get; set; }
	
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
	
	[Button("Generate Environment")]
	public void GenerateEnvironment(EnvironmentID environmentID)
	{
		// Check if environment is already generated
		if(_environmentList.Count > 0)
		{
			foreach (EnvironmentID environment in _environmentList)
			{
				if(environment == environmentID)
				{
					// Environment found, should not generate an environment already found
					Debug.LogError("Should not be trying to generate an environment that is already generated");
					return;
				}
			}
		}
		
		// Generate environment based on ID
		switch(environmentID)
		{
			case EnvironmentID.Forest:
				_environmentList.Add(EnvironmentID.Forest);
				GetComponent<ForestGeneration>().GenerateForest();
				ACTIVE_ENVIRONMENT_ID = EnvironmentID.Forest;
				break;
			case EnvironmentID.Cave:
				_environmentList.Add(EnvironmentID.Cave);
				GetComponent<CaveGeneration>().GenerateCave();
				ACTIVE_ENVIRONMENT_ID = EnvironmentID.Cave;
				break;
		}
	}
	
	[Button("Load Environment")]
	public async void LoadEnvironment(EnvironmentID environmentID, Vector2 portalPosition)
	{
		if(environmentID == ACTIVE_ENVIRONMENT_ID)
		{
			Debug.LogError($"Should not be trying to load an environment you are already in. environmentID: {environmentID}, ACTIVE_ENVIRONMENT_ID: {ACTIVE_ENVIRONMENT_ID}");
			return;
		}
		
		_isTransitioningEnvironment = true;
		
		// NTFS: make sure player is not able to move during this process and add a loading screen
		
		// Teleport player to portal he is entering
		PlacePlayerAt(portalPosition);
		
		// Clear all player chunks
		ChunkManager.Instance.UnloadAllPlayerChunks();
		AssetManager.Instance.ClearAllEnvironmentObjects();
		// NpcSpawnManager.Instance.ClearAllEntities();
		
		// Save the current environment to file
		await SaveSystem.Instance.SerializeDataAndWriteToFile();
		
		// Change environment
		ACTIVE_ENVIRONMENT_ID = environmentID;
		
		// Load or generate new environment data depending on the environment
		await SaveSystem.Instance.DeserializeAndDispatchData();

		_isTransitioningEnvironment = false;
		
		ChunkManager.Instance.UpdateChunksAroundPlayer();
		
		// If there is a portal in lets say a 10 tile radius, grab it's position, and teleport player to that portal
		// Define the radius for the portal search area
		float searchRadius = 10f; // Adjust radius as needed

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
		AssetManager.Instance.PlaceResourceAsset(v2IntPos, _portalObjectPrefab);
	}
	
	private void PlacePlayerAt(Vector2 portalPosition)
	{
		Player.LocalClientInstance.transform.SetPositionAndRotation(new(portalPosition.x + 0.5f, portalPosition.y - 0.5f), Quaternion.identity);
	}
	
	private void Tick()
	{
		_currentTime += Time.deltaTime;

		while (_currentTime > _dayDurationInSeconds)
			_currentTime -= _dayDurationInSeconds;
			
		// if(DayCycleHandler != null)
		// 	DayCycleHandler.Tick();
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
				Environment.Instance.GetWallTilemapData().DeleteTile(new(neighborPosition.x, neighborPosition.y));
			}
		}
	}
	
	/// <summary>
	/// Return in the format "xx:xx" the given ration (between 0 and 1) of time
	/// </summary>
	/// <param name="ratio"></param>
	/// <returns></returns>
	public static string GetTimeAsString(float ratio)
	{
		var hour = GetHourFromRatio(ratio);
		var minute = GetMinuteFromRatio(ratio);

		return $"{hour}:{minute:00}";
	}
	
	public static int GetHourFromRatio(float ratio)
	{
		var time = ratio * 24.0f;
		var hour = Mathf.FloorToInt(time);

		return hour;
	}

	public static int GetMinuteFromRatio(float ratio)
	{
		var time = ratio * 24.0f;
		var minute = Mathf.FloorToInt((time - Mathf.FloorToInt(time)) * 60.0f);

		return minute;
	}
	
	public bool GetIsTransitioningEnvironment()
	{
		return _isTransitioningEnvironment;
	}
	
	public EnvironmentID GetActiveEnvironmentID()
	{
		return ACTIVE_ENVIRONMENT_ID;
	}
}
