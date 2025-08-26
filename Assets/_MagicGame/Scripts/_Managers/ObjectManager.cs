using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ResourceManager : NetworkBehaviour
{
	public static ResourceManager Instance { get; private set; }
	
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
	public void DestroyResourceServerRpc(BiomeType biome, Vector2Int resourcePos, ushort resourceId)
	{
		foreach (ResourceObjectGameData objectGameData in ChunkManager.Instance.GetChunkFromAnyWorldPos(resourcePos, biome).GetWorldObjects())
		{
			if(objectGameData.Position != resourcePos) continue;
		
			if (ChestManager.Instance.GetChestDataFromBiome(biome).ContainsKey(resourcePos))
			{
				if (ChestManager.Instance.OpenedChestIds.Contains($"{resourcePos}{biome}") || ChestManager.Instance.ChestHasItems(resourcePos, biome))
				{
					Debug.LogWarning("Can't destroy a chest that is not empty or open.");
					return;
				}
			}
			
			ResourceDataSO rscData = GameDataRegistry.Instance.GetResourceDataFromUShortId(resourceId);
			if (!rscData.PassThrough)
			{
				Pathfinding.Instance.RemovePathfindingfWallTileServerRpc(resourcePos, biome);
			}
			
			LootTable.SpawnLoot(rscData.Table, (Vector2)resourcePos + (Vector2.one * 0.5f), biome);
			ChunkManager.Instance.RemoveRscDataFromChunkServerRpc(resourcePos, biome);

			return;
		}
	}
	
	public void ClearAllBiomeObjectVisuals()
	{
		OnClearAllEnvironmentObjects?.Invoke(this, new EventArgs());
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void SetDoorOpenStateServerRpc(Vector2Int doorPos, BiomeType biome, bool isOpen)
	{
		ChunkManager.Instance.SetDoorState(doorPos, biome, isOpen);

		if (isOpen)
		{
			Pathfinding.Instance.RemovePathfindingfWallTileServerRpc(Vector2Int.FloorToInt(doorPos), biome);
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
		if(biome == Player.Instance.CurrentBiome.Value)
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
	public void PlaceResourceObjectServerRpc(Vector2Int position, ushort id, BiomeType biomeToPlaceIn, CardinalDirection orientation)
	{
		// While on server, add the data to chunks
		ChunkManager.Instance.AddResourceDataToChunkServerRpc(position, id, biomeToPlaceIn, orientation);
		
		ResourceObject obj = GameDataRegistry.Instance.GetResourceDataFromUShortId(id).ResourcePrefab;
		if(!obj.Data.PassThrough)
		{
			Pathfinding.Instance.AddPfWallTileServerRpc(position, biomeToPlaceIn);
		}
		
		HandleObjectVisualsClientRpc(position, id, biomeToPlaceIn, orientation);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void HandleObjectVisualsClientRpc(Vector2Int position, ushort assetID, BiomeType objectBiome, CardinalDirection orientation)
	{
		if(objectBiome == Player.Instance.CurrentBiome.Value)
		{
			// Visually place it down for everyone
			ResourceObject worldAsset = GameDataRegistry.Instance.GetResourceDataFromUShortId(assetID).ResourcePrefab;
			GameObject placedAsset = Instantiate(worldAsset.gameObject, (Vector2)position, Quaternion.identity);
			placedAsset.GetComponent<ResourceObject>().SetOrientation(orientation);

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
		if(e.Chunk.GetWorldObjects().Count <= 0) return;
		
		foreach (ResourceObjectGameData objectData in e.Chunk.GetWorldObjects())
		{	
			// Instantiate the visual asset
			GameObject assetGO = Instantiate(objectData.Rsc.gameObject, (Vector2)objectData.Position, Quaternion.identity);
			
			assetGO.GetComponent<ResourceObject>().SetOrientation(objectData.Orientation);

			if (assetGO.TryGetComponent(out DoorObject door))
			{
				var doorObject = objectData as DoorObjectGameData;
				door.SetOrientation(doorObject.Orientation);
				door.InitializeOpenState(doorObject.IsOpen);
			}
			
			if(!objectData.Rsc.Data.PassThrough)
			{
				Pathfinding.Instance.AddPfWallTileServerRpc(objectData.Position, Player.Instance.CurrentBiome.Value);
				// TileManager.Instance.AddTileVisibilityData((Vector3Int)objectData.Position, new TileVisibility() { Visibility = 1 });
			}
			
			OnWorldObjectSpawned?.Invoke(this, new OnWorldAssetSpawnedEventArgs
			{
				WorldObjectGameObject = assetGO
			});
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		if(e.Chunk.GetWorldObjects().Count <= 0) return;
		
		foreach (ResourceObjectGameData assetData in e.Chunk.GetWorldObjects())
		{
			// If asset visually exists, just delete it
			if(TryToFindResourceObject(assetData.Position, out ResourceObject wo))
			{
				// NTFS: This might be bugged
				Destroy(wo);
			}
		}
	}
	
	public bool TryToFindResourceObject(Vector2Int position, out ResourceObject wo)
	{
		// Convert the tile position to world space if necessary
		Vector2 worldPosition = (Vector2)position + new Vector2(0.5f, 0.5f); // Center of the tile

		// Use OverlapPointAll to check for all colliders at the position
		Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition);

		// Iterate through the colliders to check for a WorldAsset component
		foreach (Collider2D collider in colliders)
		{
			collider.TryGetComponent(out ResourceObject asset);
			
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
		ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk -= ChunkManager_OnUnloadChunk;
	}
}
