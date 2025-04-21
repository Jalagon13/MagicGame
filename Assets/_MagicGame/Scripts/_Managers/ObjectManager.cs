using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ObjectManager : NetworkBehaviour
{
	public static ObjectManager Instance { get; private set; }
	
	public event EventHandler OnClearAllEnvironmentObjects;
	public event EventHandler<OnWorldAssetSpawnedEventArgs> OnWorldObjectSpawned;
	public class OnWorldAssetSpawnedEventArgs : EventArgs 
	{
		public GameObject WorldObjectGameObject;
	}
	
	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void DestroyObjectServerRpc(BiomeType biome, Vector2Int objectPos, int id)
	{
		foreach (WorldObjectGameData objectGameData in ChunkManager.Instance.GetChunkFromAnyWorldPos(objectPos, biome).WorldObjectGameDataList)
		{
			if(objectGameData.Position != objectPos) continue;
		
			if (ChestManager.Instance.GetChestDataFromBiome(biome).ContainsKey(objectPos))
			{
				if (ChestManager.Instance.OpenedChestIds.Contains($"{objectPos}{biome}") || ChestManager.Instance.ChestHasItems(objectPos, biome))
				{
					Debug.LogWarning("Can't destroy a chest that is not empty or open.");
					return;
				}
			}

			objectGameData.WO.DestroyObject(objectPos, biome);
			return;
		}
	}
	
	public void ClearAllEnvironmentObjectVisuals()
	{
		OnClearAllEnvironmentObjects?.Invoke(this, new EventArgs());
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void SetDoorOpenStateServerRpc(Vector2Int doorPos, BiomeType biome, bool isOpen)
	{
		ChunkManager.Instance.SetDoorState(doorPos, biome, isOpen);

		if (isOpen)
		{
			Pathfinding.Instance.RemovePfWallTileServerRpc(Vector2Int.FloorToInt(doorPos), biome);
		}
		else
		{
			Pathfinding.Instance.AddPfWallTileServerRpc(Vector2Int.FloorToInt(doorPos), biome);
		}
		
		HandleDoorVisualsClientRpc(doorPos, isOpen, biome);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void HandleDoorVisualsClientRpc(Vector2Int doorPos, bool isOpen, BiomeType biome)
	{
		if(biome == Player.LocalClientInstance.CurrentPlayerBiome.Value)
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

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void PlaceResourceObjectServerRpc(Vector2Int position, int id, BiomeType biomeToPlaceIn, CardinalDirection orientation)
	{
		// While on server, add the data to chunks
		ChunkManager.Instance.AddObjectDataToChunkServerRpc(position, id, biomeToPlaceIn, orientation);
		
		WorldObject obj = GameManager.Instance.GetWorldObjectFromID(id);
		if(!obj.PassThrough)
		{
			Pathfinding.Instance.AddPfWallTileServerRpc(position, biomeToPlaceIn);
		}
		
		HandleObjectVisualsClientRpc(position, id, biomeToPlaceIn, orientation);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void HandleObjectVisualsClientRpc(Vector2Int position, int assetID, BiomeType objectBiome, CardinalDirection orientation)
	{
		if(objectBiome == Player.LocalClientInstance.CurrentPlayerBiome.Value)
		{
			// Visually place it down for everyone
			WorldObject worldAsset = GameManager.Instance.GetWorldObjectFromID(assetID);
			GameObject placedAsset = Instantiate(worldAsset.gameObject, (Vector2)position, Quaternion.identity);
			placedAsset.GetComponent<WorldObject>().SetOrientation(orientation);

			if (placedAsset.TryGetComponent(out DoorObject doorObject))
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
			
			assetGO.GetComponent<WorldObject>().SetOrientation(objectData.Orientation);

			if (assetGO.TryGetComponent(out DoorObject door))
			{
				var doorObject = objectData as DoorObjectGameData;
				door.SetOrientation(doorObject.Orientation);
				door.InitializeOpenState(doorObject.IsOpen);
			}
			
			if(!objectData.WO.PassThrough)
			{
				Pathfinding.Instance.AddPfWallTileServerRpc(objectData.Position, Player.LocalClientInstance.CurrentPlayerBiome.Value);
				TileManager.Instance.AddTileVisibilityData((Vector3Int)objectData.Position, new TileVisibility() { Visibility = 1 });
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
				TileManager.Instance.RemoveTileVisibilityData((Vector3Int)assetData.Position);
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
