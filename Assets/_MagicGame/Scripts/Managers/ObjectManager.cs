using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ObjectHpData
{
	public Vector2Int ObjectPosition { get; private set; }
	public WorldObject WO { get; private set; }
	public int CurrentObjectHp { get; private set; }
	public bool IsDestroyed { get { return CurrentObjectHp <= 0;  } }
	
	private BiomeType _biome;
	
	public ObjectHpData(int objectId, BiomeType biome, Vector2Int objectPos)
	{
		WO = GameManager.Instance.GetWorldObjectFromID(objectId);
		CurrentObjectHp = WO.MaxHp;
		ObjectPosition = objectPos;
		_biome = biome;
	}
	
	public void DamageObject(int amount)
	{
		CurrentObjectHp -= amount;
		
		var spawnPos = new Vector2(ObjectPosition.x + 0.5f, ObjectPosition.y + 0.5f);
		SoundManager.Instance.PlayOneShot(WO.ResourceHit, spawnPos);
	}
	
	public void DestroyObject()
	{
		WO.DestroyObject(ObjectPosition, _biome);
	}
}

public class ObjectManager : NetworkBehaviour
{
	public static ObjectManager Instance { get; private set; }
	
	public event EventHandler OnClearAllEnvironmentObjects;
	public event EventHandler<OnWorldAssetSpawnedEventArgs> OnWorldObjectSpawned;
	public class OnWorldAssetSpawnedEventArgs : EventArgs 
	{
		public GameObject WorldObjectGameObject;
	}
	
	private Dictionary<BiomeType, HashSet<ObjectHpData>> _biomeObjectHpDict = new();
	
	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
	}
	
	public void HitObject(BiomeType biome, WorldObject wo, int amount)
	{
		HitObjectServerRpc(biome, Vector2Int.FloorToInt(wo.transform.position), amount, GameManager.Instance.GetIDFromWorldObject(wo));
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void HitObjectServerRpc(BiomeType biome, Vector2Int objectPos, int amount, int id)
	{
		var chunkGameData = ChunkManager.Instance.GetChunkFromAnyWorldPos(objectPos, biome);
		
		foreach (WorldObjectGameData woGameData in chunkGameData.WorldObjectGameDataList)
		{
			if(woGameData.Position == objectPos)
			{
				string chestId = $"{objectPos}{biome}";
				if (ChestManager.Instance.OpenedChestIds.Contains(chestId))
				{
					Debug.LogWarning("Trying to damage a chest that is open is not allowed");
					return;
				}

				// Found object to hit
				if (_biomeObjectHpDict.ContainsKey(biome))
				{
					// Try to find tile to damage
					foreach (ObjectHpData objectHpData in _biomeObjectHpDict[biome])
					{
						if(objectHpData.ObjectPosition == objectPos)
						{
							// Found tile to damage, so damage it
							DamageObject(biome, amount, objectHpData);
							return;
						}
					}
			
					// Did not find tile to damage, create a new one, damage it
					DamageObject(biome, amount, new ObjectHpData(id, biome, objectPos));
				}
				else
				{
					// Biome does not exist, create it and add tile entry
					_biomeObjectHpDict.Add(biome, new());
					DamageObject(biome, amount, new ObjectHpData(id, biome, objectPos));
			
					if(_biomeObjectHpDict[biome].Count <= 0)
					{
						_biomeObjectHpDict.Remove(biome);
					}
				}
				return;
			}
		}
	
		Debug.LogWarning($"Did not find wall tile to hit at {objectPos} in biome {biome}");
	}
	
	private void DamageObject(BiomeType biome, int amount, ObjectHpData objectToDamage)
	{
		objectToDamage.DamageObject(amount);
		
		if(objectToDamage.IsDestroyed)
		{
			objectToDamage.DestroyObject();
			
			// Check if tile exists in database, if so remove it
			foreach (ObjectHpData objectHpData in _biomeObjectHpDict[biome].ToList())
			{
				if(objectHpData.ObjectPosition == objectToDamage.ObjectPosition)
				{
					// Found tile to destroy, delete it from the database
					_biomeObjectHpDict[biome].Remove(objectHpData);
				}
			}
		}
		else
		{
			_biomeObjectHpDict[biome].Add(objectToDamage);
		}
	}
	
	public void ClearAllEnvironmentObjectVisuals()
	{
		OnClearAllEnvironmentObjects?.Invoke(this, new EventArgs());
	}
	
	public void ToggleDoor(Vector2Int doorPos, BiomeType biome)
	{
		ToggleDoorServerRpc(doorPos, biome);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void ToggleDoorServerRpc(Vector2Int doorPos, BiomeType biome)
	{
		bool newIsOpen = ChunkManager.Instance.ToggleDoor(doorPos, biome);
		HandleDoorVisualsClientRpc(doorPos, newIsOpen, biome);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void HandleDoorVisualsClientRpc(Vector2Int doorPos, bool isOpen, BiomeType biome)
	{
		if(biome == Player.LocalClientInstance.CurrentBiome.Value)
		{
			// If there exists a door in this position, set its open value to isOpen
			var colliders = Physics2D.OverlapPointAll(doorPos + new Vector2(0.5f, 0.5f));
			Debug.Log($"Door pos checking: {doorPos}, center of door {doorPos + new Vector2(0.5f, 0.5f)}");
			foreach (var collider in colliders)
			{
				if(collider.TryGetComponent(out DoorObject doorObject))
				{
					Debug.Log($"Found door, setting it open to: {isOpen}");
					doorObject.SetIsOpen(isOpen);
					return;
				}
			}
			
			Debug.LogError($"No door found here. There should be a door here");
		}
	}

	public void PlaceObject(Vector2Int position, WorldObject worldObject, BiomeType environmentToPlaceIn)
	{
		PlaceResourceObjectServerRpc(position, GameManager.Instance.GetIDFromWorldObject(worldObject), environmentToPlaceIn);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void PlaceResourceObjectServerRpc(Vector2Int position, int id, BiomeType biomeToPlaceIn)
	{
		// While on server, add the data to chunks
		WorldObject obj = GameManager.Instance.GetWorldObjectFromID(id);
		ChunkManager.Instance.AddObjectDataToChunk(position, obj, biomeToPlaceIn);
		
		if(!obj.PassThrough)
		{
			Pathfinding.Instance.AddPfWallTile(position, biomeToPlaceIn);
		}
		
		HandleObjectVisualsClientRpc(position, id, biomeToPlaceIn);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void HandleObjectVisualsClientRpc(Vector2Int position, int assetID, BiomeType objectBiome)
	{
		if(objectBiome == Player.LocalClientInstance.CurrentBiome.Value && ChunkManager.Instance.ObjectPositionInLoadedChunks(position))
		{
			// Visually place it down for everyone
			WorldObject worldAsset = GameManager.Instance.GetWorldObjectFromID(assetID);
			GameObject placedAsset = Instantiate(worldAsset.gameObject, (Vector2)position, Quaternion.identity);

			if(placedAsset.TryGetComponent(out DoorObject doorObject))
			{
				doorObject.InitializeOpenState(false);
			}
			
			OnWorldObjectSpawned?.Invoke(this, new OnWorldAssetSpawnedEventArgs
			{
				WorldObjectGameObject = placedAsset
			});
		}
	}

	private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		if(e.Chunk.WorldObjectGameDataList.Count <= 0) return;
		
		foreach (WorldObjectGameData objectData in e.Chunk.WorldObjectGameDataList)
		{	
			// Instantiate the visual asset
			GameObject assetGO = Instantiate(objectData.WO.gameObject, (Vector2)objectData.Position, Quaternion.identity);
			
			if(assetGO.TryGetComponent(out DoorObject doorObject))
			{
				doorObject.InitializeOpenState((objectData as DoorObjectGameData).IsOpen);
			}
			
			if(!objectData.WO.PassThrough)
			{
				Pathfinding.Instance.AddPfWallTile(objectData.Position, Player.LocalClientInstance.CurrentBiome.Value);
				Environment.Instance.AddTileVisData((Vector3Int)objectData.Position, new TileVisibility() { Visibility = 1 });
			}
			
			OnWorldObjectSpawned?.Invoke(this, new OnWorldAssetSpawnedEventArgs
			{
				WorldObjectGameObject = assetGO
			});
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		if(e.Chunk.WorldObjectGameDataList.Count <= 0) return;
		
		foreach (WorldObjectGameData assetData in e.Chunk.WorldObjectGameDataList)
		{
			// If asset visually exists, just delete it
			if(TryToFindWorldObject(assetData.Position, out WorldObject wo))
			{
				Environment.Instance.RemoveTileVisData((Vector3Int)assetData.Position);
				wo.DestroySelf();
			}
		}
	}
	
	public bool TryToFindWorldObject(Vector2Int position, out WorldObject wo)
	{
		// Convert the tile position to world space if necessary
		Vector2 worldPosition = (Vector2)position + new Vector2(0.5f, 0.5f); // Center of the tile

		// Use OverlapPointAll to check for all colliders at the position
		Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition);

		// Iterate through the colliders to check for a WorldAsset component
		foreach (Collider2D collider in colliders)
		{
			collider.TryGetComponent(out WorldObject asset);
			
			if (asset != null)
			{
				wo = asset;
				return true; // Found a WorldAsset component
			}
		}

		// No matching collider with a WorldAsset component was found
		wo = null;
		return false;
	}
	
	public override void OnDestroy()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
	}
}
