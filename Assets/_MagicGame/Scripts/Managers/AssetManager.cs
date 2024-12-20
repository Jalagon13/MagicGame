using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.Netcode;
using UnityEngine;

public class AssetManager : NetworkBehaviour
{
	public static AssetManager Instance;
	
	public event EventHandler OnClearAllEnvironmentObjects;
	public event EventHandler<OnWorldAssetSpawnedEventArgs> OnWorldAssetSpawned;
	public class OnWorldAssetSpawnedEventArgs : EventArgs 
	{
		public GameObject WorldAssetGameObject;
	}
	
	private NetworkList<SyncWorldAssetHPData> _syncWorldAssetDataHPNetworkList = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public struct SyncWorldAssetHPData : IEquatable<SyncWorldAssetHPData>, INetworkSerializable
	{
		public byte WorldAssetID;
		public ushort CurrentWorldAssetHP;
		public Vector2Int Position;

		public bool Equals(SyncWorldAssetHPData other)
		{
			return Position.Equals(other.Position) && WorldAssetID == other.WorldAssetID;
		}
	
		public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
		{
			serializer.SerializeValue(ref CurrentWorldAssetHP);
			serializer.SerializeValue(ref Position);
			serializer.SerializeValue(ref WorldAssetID);
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
		if(e.Chunk.WorldAssetGameDataList.Count <= 0) return;
		
		foreach (WorldAssetGameData assetData in e.Chunk.WorldAssetGameDataList)
		{	
			// Instantiate the visual asset
			GameObject assetGO = Instantiate(assetData.Asset.gameObject, (Vector2)assetData.Position, Quaternion.identity);
			
			OnWorldAssetSpawned?.Invoke(this, new OnWorldAssetSpawnedEventArgs
			{
				WorldAssetGameObject = assetGO
			});
		}
	}

	private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
	{
		if(e.Chunk.WorldAssetGameDataList.Count <= 0) return;
		
		foreach (WorldAssetGameData assetData in e.Chunk.WorldAssetGameDataList)
		{
			// If asset visually exists, just delete it
			if(ResourceAssetFoundAtPosition(assetData.Position, out ResourceObject resourceObject))
			{
				resourceObject.DestroySelf();
			}
		}
	}
	
	public void ClearAllEnvironmentObjectVisuals()
	{
		OnClearAllEnvironmentObjects?.Invoke(this, new EventArgs());
	}
	
	public void PlaceResourceAsset(Vector2Int position, WorldObject worldObject)
	{
		byte assetID = GameManager.Instance.GetByteIDFromWorldObject(worldObject);
	
		PlaceResourceAssetServerRpc(position, assetID);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void PlaceResourceAssetServerRpc(Vector2Int position, byte assetID)
	{
		// While on server, add the data to chunks
		WorldObject asset = GameManager.Instance.GetWorldObjectFromID(assetID);
		ChunkManager.Instance.AddWorldAssetDataToChunk(position, asset);
		
		PlaceResourceAssetClientRpc(position, assetID);
	}

	[Rpc(SendTo.Everyone)]
	private void PlaceResourceAssetClientRpc(Vector2Int position, byte assetID)
	{
		// Visually place it down for everyone
		WorldObject worldAsset = GameManager.Instance.GetWorldObjectFromID(assetID);
		
		GameObject placedAsset = Instantiate(worldAsset.gameObject, (Vector2)position, Quaternion.identity);
		placedAsset.GetComponent<WorldObject>().SetPlacedDownByPlayer(true);
		
		OnWorldAssetSpawned?.Invoke(this, new OnWorldAssetSpawnedEventArgs
		{
			WorldAssetGameObject = placedAsset
		});
	}

	public void HitResourceAsset(Vector2Int position, ushort incomingDamage)
	{
		if(ResourceAssetFoundAtPosition(position, out ResourceObject resourceObjectFound))
		{
			byte assetID = GameManager.Instance.GetByteIDFromWorldObject(resourceObjectFound);
			
			DamageWorldAssetServerRpc(position, assetID, incomingDamage);
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DamageWorldAssetServerRpc(Vector2Int position, byte assetID, ushort incomingDamage)
	{
		if(!SyncWorldAssetHPDataListContainsPosition(position))
		{
			AddAssetToNetworkListDamaged(position, assetID, incomingDamage);
			return;
		}
		
		for (int i = 0; i < _syncWorldAssetDataHPNetworkList.Count; i++)
		{
			var syncWorldAssetHpData = _syncWorldAssetDataHPNetworkList[i];

			if (syncWorldAssetHpData.Position == position)
			{
				// If damage is greater than current hp for this incoming attack, destroy the tile
				if (incomingDamage > syncWorldAssetHpData.CurrentWorldAssetHP)
				{
					// Remove the tile if destroyed
					_syncWorldAssetDataHPNetworkList.RemoveAt(i);
					
					// Trigger tile destruction logic
					DestroyAssetClientRpc(position);
				}
				else
				{
					// Update the modified struct in the list
					syncWorldAssetHpData.CurrentWorldAssetHP -= incomingDamage;
					// Debug.Log("Found tile callback, tile hp after damage: " + syncTileHpData.CurrentTileHP);
					_syncWorldAssetDataHPNetworkList[i] = syncWorldAssetHpData;
				}

				return; // Exit after finding the tile
			}
		}
	}

	private void AddAssetToNetworkListDamaged(Vector2Int position, byte assetID, ushort damageAmount)
	{
		ResourceObject resourceAsset = GameManager.Instance.GetWorldObjectFromID(assetID) as ResourceObject;
		
		ushort currentAssetHPAfterDamage = (ushort)(resourceAsset.GetMaxHitPoints() - damageAmount);
		if(currentAssetHPAfterDamage > 0)
		{
			// If tile hp after damage is above 0, just add as usual
			_syncWorldAssetDataHPNetworkList.Add(new SyncWorldAssetHPData()
			{
				WorldAssetID = GameManager.Instance.GetByteIDFromWorldObject(resourceAsset),
				CurrentWorldAssetHP = currentAssetHPAfterDamage,
				Position = position
			});
		}
		else
		{
			// If tile hp is destroyed, destroy tile
			DestroyAssetClientRpc(position);
		}
	}

	[Rpc(SendTo.Everyone)]
	private void DestroyAssetClientRpc(Vector2Int position)
	{
		// If resource is not found that means it is disabled and therefore should not be destroyed, if so it is enabled and should be destroyed
		if(ResourceAssetFoundAtPosition(position, out ResourceObject resourceObjectFound))
		{
			resourceObjectFound.DestroyResourceAsset();
		}
		
		// Handle internal data deletion here
		ChunkManager.Instance.RemoveWorldAssetDataFromChunk(position);
	}

	private bool SyncWorldAssetHPDataListContainsPosition(Vector2Int position)
	{
		foreach (SyncWorldAssetHPData hpData in _syncWorldAssetDataHPNetworkList)
		{
			if(hpData.Position == position)
			{
				return true;
			}
		}
		
		return false;
	}

	private bool ResourceAssetFoundAtPosition(Vector2Int position, out ResourceObject resourceObject)
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
	
	public NetworkList<SyncWorldAssetHPData> GetSyncWorldAssetDataHPNetworkList()
	{
		return _syncWorldAssetDataHPNetworkList;
	}
	
	public override void OnDestroy()
	{
		ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
		ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
	}
}
