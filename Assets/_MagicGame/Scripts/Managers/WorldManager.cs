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
	[SerializeField] private Portal _portalPrefab;
	
	// Class globals
	private List<EnvironmentID> _environmentList = new(); // Used to keep track which environments have been generated or not
	private float _currentTime;
	private bool _anEnvironmentIsActive;
	
	// Properties
	public Dictionary<string, List<Vector2Int>> StaircasePositionsByScene = new();
	// public DayCycleHandler DayCycleHandler { get; set; }
	// public LightMap LightMap { get; set; }
	public float CurrentDayRatio => _currentTime / _dayDurationInSeconds;
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
		
		foreach (EnvironmentID id in Enum.GetValues(typeof(EnvironmentID)))
		{
			// _environmentPortalDataDict.Add(id, new());
		}
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
	public async void LoadEnvironment(EnvironmentID environmentID, string portalID)
	{
		if(environmentID == ACTIVE_ENVIRONMENT_ID)
		{
			Debug.LogError($"Should not be trying to load an environment you are already in. environmentID: {environmentID}, ACTIVE_ENVIRONMENT_ID: {ACTIVE_ENVIRONMENT_ID}");
			return;
		}
		
		_anEnvironmentIsActive = false;
		
		// NTFS: make sure player is not able to move during this process and add a loading screen
		
		// Save the current environment to file
		await SaveSystem.Instance.SerializeDataAndWriteToFile();
		
		// Clear all Entities
		// NpcSpawnManager.Instance.ClearAllEntities();
		
		// Clear all active assets
		// AssetManager.Instance.ClearAllCurrentEnvironmentAssets();
		
		// Clear all player chunks
		ChunkManager.Instance.UnloadAllPlayerChunks();
		
		// Change environment
		ACTIVE_ENVIRONMENT_ID = environmentID;
		
		// Load or generate new environment data depending on the environment
		await SaveSystem.Instance.DeserializeAndDispatchData();
		
		// Portal spawning logic
		SpawnPortals();
		SpawnPlayerAtPortal(portalID);
		
		_anEnvironmentIsActive = true;
	}
	
	private void SpawnPlayerAtPortal(string portalID)
	{
		// foreach (PortalData portalData in _environmentPortalDataDict[ACTIVE_ENVIRONMENT_ID])
		// {
		// 	if(portalData.PortalID == portalID)
		// 	{
		// 		// Spawn player at this location
		// 		Player.LocalClientInstance.transform.SetPositionAndRotation(new(portalData.PortalPosition.x + 0.5f, portalData.PortalPosition.y - 0.5f), Quaternion.identity);
		// 	}
		// }
	}
	
	private void SpawnPortals()
	{
		// Debug.Log($"Number Of Portals in {ACTIVE_ENVIRONMENT_ID}: {_environmentPortalDataDict[ACTIVE_ENVIRONMENT_ID].Count}");
		// foreach (PortalData portalData in _environmentPortalDataDict[ACTIVE_ENVIRONMENT_ID])
		// {
		// 	if(!portalData.IsDestructable)
		// 	{
		// 		if(!PortalExistsAt(portalData.PortalPosition))
		// 		{
		// 			// Create copy of this portal, set the appropriate destination, and make it nondestructable
		// 			GameObject portalGameObject = Instantiate(_portalPrefab.gameObject, portalData.PortalPosition, Quaternion.identity);
		// 			Portal portal = portalGameObject.GetComponent<Portal>();

		// 			portal.SetDestructable(portalData.IsDestructable);
		// 			portal.SetDestination(portalData.DestinationID);
					
		// 			// Destroy tiles around the portal
		// 			DeleteNeighborWallsAroundPoint(portalData.PortalPosition);
		// 		}
		// 	}
		// }
	}
	
	// public bool PortalDataPositionExistsAt(Vector3 position, out PortalData portalDataInstance)
	// {
	// 	// foreach (PortalData portalData in _environmentPortalDataDict[ACTIVE_ENVIRONMENT_ID])
	// 	// {
	// 	// 	if(portalData.PortalPosition == position)
	// 	// 	{
	// 	// 		// Portal already exists at this location
	// 	// 		portalDataInstance = portalData;
	// 	// 		return true;
	// 	// 	}
	// 	// }
		
	// 	portalDataInstance = default;
	// 	return false;
	// }
	
	private bool PortalExistsAt(Vector3 position)
	{
		Collider2D[] colliders = Physics2D.OverlapPointAll(new(position.x + 0.5f, position.y + 0.5f));
		for (int i = 0; i < colliders.Length; i++)
		{
			var portal = colliders[i].GetComponent<Portal>();
			if(portal != null)
			{
				return true;
			}
		}
		
		return false;
	}
	
	public void LinkPortal(EnvironmentID destinationEnvironmentID, string uniqueID, Vector3 portalPosition)
	{
		// Register portal to destination environment
		AddPortalLink(ACTIVE_ENVIRONMENT_ID, destinationEnvironmentID, true, uniqueID, portalPosition);
		
		// Register a portal in destination environment to current environment
		AddPortalLink(destinationEnvironmentID, ACTIVE_ENVIRONMENT_ID, false, uniqueID, portalPosition);
		Debug.Log("Portals Linked");
	}
	
	public void UnLinkPortal(string portalID)
	{
		// for (int i = _environmentPortalDataDict[ACTIVE_ENVIRONMENT_ID].Count - 1; i >= 0; i--)
		// {
		// 	PortalData startLinkPortalData = _environmentPortalDataDict[ACTIVE_ENVIRONMENT_ID][i];
			
		// 	if(startLinkPortalData.PortalID == portalID)
		// 	{
		// 		for (int y = _environmentPortalDataDict[startLinkPortalData.DestinationID].Count - 1; y >= 0; y--)
		// 		{
		// 			PortalData endLinkPortalData = _environmentPortalDataDict[startLinkPortalData.DestinationID][y];
					
		// 			if(startLinkPortalData.PortalID == endLinkPortalData.PortalID)
		// 			{
		// 				_environmentPortalDataDict[startLinkPortalData.DestinationID].Remove(endLinkPortalData);
		// 				_environmentPortalDataDict[ACTIVE_ENVIRONMENT_ID].Remove(startLinkPortalData);
		// 				Debug.Log("Portals to unlink found and unlinked");
		// 				return;
		// 			}
		// 		}
		// 	}
		// }
		
		// Debug.LogError("Did not find any portals to unlink");
	}
	
	private void AddPortalLink(EnvironmentID startLinkEnvironment, EnvironmentID endLinkEnvironment, bool isDestructable, string uniqueID, Vector3 portalPosition)
	{	
		// _environmentPortalDataDict[startLinkEnvironment].Add(new PortalData()
		// {
		// 	PortalID = uniqueID,
		// 	DestinationID = endLinkEnvironment,
		// 	IsDestructable = isDestructable,
		// 	PortalPosition = portalPosition
		// });
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
	
	public EnvironmentID GetActiveEnvironmentID()
	{
		return ACTIVE_ENVIRONMENT_ID;
	}
	
	public bool GetAnEnvironmentIsActive()
	{
		return _anEnvironmentIsActive;
	}
	
	public void SetAnEnvironmentIsActive(bool var)
	{
		_anEnvironmentIsActive = var;
	}
}
