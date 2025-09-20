using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkChunkManager : NetworkBehaviour
{
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void RequestChunkDataServerRpc(ulong clientId, BiomeType requestBiome, Vector2Int chunkPosition, RpcParams rpcParams = default)
	{
		var chunkData = ChunkManager.Instance.GetChunkFromChunkPosition(requestBiome, chunkPosition);
		var syncChunkData = ConvertToSyncChunkData(chunkData);
		
		Pathfinding.Instance.UpdateChunkPathfinding(chunkPosition, chunkData, requestBiome, clientId);
		
		SendChunkDataToClientRpc(requestBiome, syncChunkData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
	}

	// TODO: Chunk and throttle map data sync to avoid UTP pipeline overload
	[Rpc(SendTo.SpecifiedInParams)]
	private void SendChunkDataToClientRpc(BiomeType requestBiome, SyncChunkData syncChunkData, RpcParams rpcParams)
	{
		if(Player.Instance.CurrentBiome.Value != requestBiome) return;
	
		ChunkGameData chunkGameData = ConvertToGameChunkData(syncChunkData);
		ChunkManager.Instance.LoadChunk(chunkGameData);
	}
	
	private SyncChunkData ConvertToSyncChunkData(ChunkGameData chunkGameData)
	{
		// Create a new SyncChunkData object, pre-sizing lists for efficiency
		int doorCount = 0;
		foreach (var worldObjectGameData in chunkGameData.GetWorldObjects())
		{
			if (worldObjectGameData is DoorObjectGameData)
			{
				doorCount++;
			}
		}
		SyncChunkData syncChunkData = new SyncChunkData
		{
			SyncChunkPosition = chunkGameData.ChunkPosition,
			
			SyncTerrainTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.GetTileList(TileType.Terrain).Count),
			SyncFloorTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.GetTileList(TileType.Floor).Count),
			SyncWallTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.GetTileList(TileType.Wall).Count),
			SyncLiquidTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.GetTileList(TileType.Liquid).Count),
			SyncFoliageTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.GetTileList(TileType.Foliage).Count),
			
			SyncObjectAssetDataList = new List<ResourceSyncData>(chunkGameData.GetWorldObjects().Count),
			SyncDoorObjectDataList = new List<DoorObjectSyncData>(doorCount)
		};

		// Convert tile game data lists to agnostic sync data using helper
		ConvertTileList(chunkGameData.GetTileList(TileType.Terrain), syncChunkData.SyncTerrainTileDataList);
		ConvertTileList(chunkGameData.GetTileList(TileType.Floor), syncChunkData.SyncFloorTileDataList);
		ConvertTileList(chunkGameData.GetTileList(TileType.Wall), syncChunkData.SyncWallTileDataList);
		ConvertTileList(chunkGameData.GetTileList(TileType.Liquid), syncChunkData.SyncLiquidTileDataList);
		ConvertTileList(chunkGameData.GetTileList(TileType.Foliage), syncChunkData.SyncFoliageTileDataList);

		// Convert world asset game data to agnostic sync data
		foreach (ResourceObjectGameData resourceGameData in chunkGameData.GetWorldObjects())
		{
			ushort id = GameDataRegistry.Instance.GetResourceIdFromResourceData(resourceGameData.Rsc.Data);
			
			if(resourceGameData is DoorObjectGameData doorObjectGameData)
			{
				syncChunkData.SyncDoorObjectDataList.Add(new DoorObjectSyncData(){ 
				Position = doorObjectGameData.Position, 
				ID = id, 
				Orientation = doorObjectGameData.Orientation, 
				IsOpen = doorObjectGameData.IsOpen});
			}
			else
			{
				syncChunkData.SyncObjectAssetDataList.Add(new ResourceSyncData(){ 
				Position = resourceGameData.Position, 
				ID = id, 
				Orientation = resourceGameData.Orientation});
			}
		}

		return syncChunkData;
	}

	// Helper function to convert tile lists to sync lists
	private void ConvertTileList(List<TileGameData> gameDataList, List<GenericGameObjectSyncData> syncDataList)
	{
		foreach (var tile in gameDataList)
		{
			ushort id = GameDataRegistry.Instance.GetTileIdFromTileData(tile.TileSO);
			syncDataList.Add(ConvertGameDataIntoGenericSyncData(tile.TilePosition, id));
		}
	}

	private GenericGameObjectSyncData ConvertGameDataIntoGenericSyncData(Vector2Int position, ushort id)
	{
		return new GenericGameObjectSyncData
		{
			Position = position,
			ID = id
		};
	}
	
private ChunkGameData ConvertToGameChunkData(SyncChunkData syncChunkData)
{
	ChunkGameData chunkGameData = new(ChunkManager.CHUNK_SIZE, syncChunkData.SyncChunkPosition);

	// Define mappings between tile types and sync data lists
	var tileDataMappings = new (TileType type, List<GenericGameObjectSyncData> syncList)[]
	{
		(TileType.Terrain, syncChunkData.SyncTerrainTileDataList),
		(TileType.Floor, syncChunkData.SyncFloorTileDataList),
		(TileType.Wall, syncChunkData.SyncWallTileDataList),
		(TileType.Liquid, syncChunkData.SyncLiquidTileDataList),
		(TileType.Foliage, syncChunkData.SyncFoliageTileDataList),
	};

	// Convert all tile types using shared logic
	foreach (var (type, syncList) in tileDataMappings)
	{
		ConvertSyncDataList(syncList, (syncTile) =>
		{
			TileDataSO tileSO = GameDataRegistry.Instance.GetTileDataFromTileId(syncTile.ID);
			return new TileGameData(tileSO, new Vector2Int(syncTile.Position.x, syncTile.Position.y));
		}, chunkGameData.GetTileList(type));
	}

	// Convert SyncWorldAssetData to WorldAssetGameData
	ConvertSyncDataList(syncChunkData.SyncObjectAssetDataList, (syncAsset) =>
	{
		ResourceObject worldObject = GameDataRegistry.Instance.GetResourceDataFromResourceId(syncAsset.ID).ResourcePrefab;
		return new ResourceObjectGameData(worldObject, syncAsset.Position, syncAsset.Orientation);
	}, chunkGameData.GetWorldObjects());

	// Convert SyncDoorObjectData To DoorObjectGameData
	ConvertSyncDataList(syncChunkData.SyncDoorObjectDataList, (syncDoor) => 
	{
		ResourceObject worldObject = GameDataRegistry.Instance.GetResourceDataFromResourceId(syncDoor.ID).ResourcePrefab;
		return new DoorObjectGameData(worldObject, syncDoor.Position, syncDoor.Orientation, syncDoor.IsOpen);
	}, chunkGameData.GetWorldObjects());

	return chunkGameData;
}

	// Generic helper function for converting Sync*DataList to *GameDataList
	private void ConvertSyncDataList<TSyncData, TGameData>(List<TSyncData> syncDataList, Func<TSyncData, TGameData> convertFunction, List<TGameData> gameDataList)
	{
		if (syncDataList == null || syncDataList.Count == 0)
		{
			return;
		}
		
		foreach (var syncData in syncDataList)
		{
			gameDataList.Add(convertFunction(syncData));
		}
	}
}