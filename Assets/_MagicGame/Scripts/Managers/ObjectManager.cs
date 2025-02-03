using System;
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
	
	private NetworkList<SyncWorldObjectHPData> _syncWorldObjectDataHPNetworkList = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public struct SyncWorldObjectHPData : IEquatable<SyncWorldObjectHPData>, INetworkSerializable
	{
		public byte WorldObjectID;
		public ushort CurrentWorldObjectHP;
		public Vector2Int Position;

		public bool Equals(SyncWorldObjectHPData other)
		{
			return Position.Equals(other.Position) && WorldObjectID == other.WorldObjectID;
		}
	
		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref CurrentWorldObjectHP);
			serializer.SerializeValue(ref Position);
			serializer.SerializeValue(ref WorldObjectID);
		}
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

	private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		if(e.Chunk.WorldObjectGameDataList.Count <= 0) return;
		
		foreach (WorldObjectGameData assetData in e.Chunk.WorldObjectGameDataList)
		{	
			// Instantiate the visual asset
			GameObject assetGO = Instantiate(assetData.Asset.gameObject, (Vector2)assetData.Position, Quaternion.identity);
			
			if(assetGO.TryGetComponent(out DoorObject doorObject))
			{
				assetGO.GetComponent<DoorObject>().InitializeOpenState((assetData as DoorObjectGameData).IsOpen);
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
			if(ResourceObjectFoundAtPosition(assetData.Position, out ResourceObject resourceObject))
			{
				resourceObject.DestroySelf();
			}
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
		byte assetID = GameManager.Instance.GetByteIDFromWorldObject(worldObject);
		PlaceResourceObjectServerRpc(position, assetID, environmentToPlaceIn);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void PlaceResourceObjectServerRpc(Vector2Int position, byte assetID, BiomeType environmentToPlaceIn)
	{
		// While on server, add the data to chunks
		WorldObject asset = GameManager.Instance.GetWorldObjectFromID(assetID);
		ChunkManager.Instance.AddObjectDataToChunk(position, asset, environmentToPlaceIn);
		HandleObjectVisualsClientRpc(position, assetID, environmentToPlaceIn);
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void HandleObjectVisualsClientRpc(Vector2Int position, byte assetID, BiomeType objectBiome)
	{
		if(objectBiome == Player.LocalClientInstance.CurrentBiome.Value /* && ObjectPositionInLoadedChunks(position) */)
		{
			// Visually place it down for everyone
			WorldObject worldAsset = GameManager.Instance.GetWorldObjectFromID(assetID);
			GameObject placedAsset = Instantiate(worldAsset.gameObject, (Vector2)position, Quaternion.identity);
			placedAsset.GetComponent<WorldObject>().SetPlacedDownByPlayer(true);
		
			OnWorldObjectSpawned?.Invoke(this, new OnWorldAssetSpawnedEventArgs
			{
				WorldObjectGameObject = placedAsset
			});
		}
	}
	
	private bool ObjectPositionInLoadedChunks(Vector2Int position)
	{
		var minLoadedTilePos = ChunkManager.Instance.MinLoadedTilePosition;
		var maxLoadedTilePos = ChunkManager.Instance.MaxLoadedTilePosition;

		// Check if the position is within the bounds
		return position.x >= minLoadedTilePos.x && position.x <= maxLoadedTilePos.x &&
			   position.y >= minLoadedTilePos.y && position.y <= maxLoadedTilePos.y;
	}

	public void DamageObject(Vector2Int position, ushort incomingDamage, BiomeType environment)
	{
		if(ResourceObjectFoundAtPosition(position, out ResourceObject resourceObjectFound))
		{
			byte assetID = GameManager.Instance.GetByteIDFromWorldObject(resourceObjectFound);
			var asset = GameManager.Instance.GetWorldObjectFromID(assetID);
			var pos = new Vector3(position.x, position.y);
			
			SoundManager.Instance.PlayOneShot(asset.ResourceHit, pos);
			
			// If hitting a chest that is opened or the chest has items, don't do anything
			if(resourceObjectFound is ChestObject)
			{
				if(ChestManager.Instance.OpenedChestIds.Contains($"{position}{environment}") || ChestManager.Instance.GetChestDataFromEnvironment(environment)[position].Count > 0) return; 
			}
			
			DamageWorldObjectServerRpc(position, assetID, incomingDamage, environment);
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DamageWorldObjectServerRpc(Vector2Int position, byte assetID, ushort incomingDamage, BiomeType environment)
	{
		if(!SyncWorldObjectHPDataListContainsPosition(position))
		{
			AddObjectToNetworkListDamaged(position, assetID, incomingDamage, environment);
			return;
		}
		
		for (int i = 0; i < _syncWorldObjectDataHPNetworkList.Count; i++)
		{
			var syncWorldObjectHpData = _syncWorldObjectDataHPNetworkList[i];

			if (syncWorldObjectHpData.Position == position)
			{
				// If damage is greater than current hp for this incoming attack, destroy the tile
				if (incomingDamage > syncWorldObjectHpData.CurrentWorldObjectHP)
				{
					// Remove the object if destroyed
					_syncWorldObjectDataHPNetworkList.RemoveAt(i);
					
					// Trigger tile destruction logic
					ChunkManager.Instance.RemoveObjectDataFromChunk(position, environment);
					DestroyObjectVisualsClientRpc(position);
				}
				else
				{
					// Update the modified struct in the list
					syncWorldObjectHpData.CurrentWorldObjectHP -= incomingDamage;
					
					_syncWorldObjectDataHPNetworkList[i] = syncWorldObjectHpData;
				}

				return; // Exit after finding the object
			}
		}
	}

	private void AddObjectToNetworkListDamaged(Vector2Int position, byte assetID, ushort damageAmount, BiomeType environment)
	{
		ResourceObject resourceAsset = GameManager.Instance.GetWorldObjectFromID(assetID) as ResourceObject;

		// Perform calculation using a signed integer
		int currentAssetHPAfterDamage = resourceAsset.GetMaxHitPoints() - damageAmount;

		if (currentAssetHPAfterDamage > 0)
		{
			// If tile hp after damage is above 0, just add as usual
			_syncWorldObjectDataHPNetworkList.Add(new SyncWorldObjectHPData()
			{
				WorldObjectID = GameManager.Instance.GetByteIDFromWorldObject(resourceAsset),
				CurrentWorldObjectHP = (ushort)currentAssetHPAfterDamage, // Cast back to ushort
				Position = position
			});
		}
		else
		{
			// If tile hp is destroyed, destroy tile
			ChunkManager.Instance.RemoveObjectDataFromChunk(position, environment);
			DestroyObjectVisualsClientRpc(position);
		}
	}

	[Rpc(SendTo.ClientsAndHost)]
	private void DestroyObjectVisualsClientRpc(Vector2Int position)
	{
		// If resource is not found that means it is disabled and therefore should not be destroyed, if so it is enabled and should be destroyed
		if(ResourceObjectFoundAtPosition(position, out ResourceObject resourceObjectFound))
		{
			resourceObjectFound.DestroyResourceAsset();
		}
	}

	private bool SyncWorldObjectHPDataListContainsPosition(Vector2Int position)
	{
		foreach (SyncWorldObjectHPData hpData in _syncWorldObjectDataHPNetworkList)
		{
			if(hpData.Position == position)
			{
				return true;
			}
		}
		
		return false;
	}

	private bool ResourceObjectFoundAtPosition(Vector2Int position, out ResourceObject resourceObject)
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
				resourceObject = asset;
				return true; // Found a WorldAsset component
			}
		}

		// No matching collider with a WorldAsset component was found
		resourceObject = null;
		return false;
	}
	
	public NetworkList<SyncWorldObjectHPData> GetSyncWorldObjectDataHPNetworkList()
	{
		return _syncWorldObjectDataHPNetworkList;
	}
	
	public override void OnDestroy()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
	}
}
