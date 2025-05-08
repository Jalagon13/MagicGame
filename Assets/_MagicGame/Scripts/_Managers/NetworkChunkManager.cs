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
		if(Player.LocalClientInstance.CurrentPlayerBiome.Value != requestBiome) return;
	
		ChunkGameData chunkGameData = ConvertToGameChunkData(syncChunkData);
		ChunkManager.Instance.LoadChunk(chunkGameData);
	}
	
	private SyncChunkData ConvertToSyncChunkData(ChunkGameData chunkGameData)
	{
		// Create a new SyncChunkData object, pre-sizing lists for efficiency
		int doorCount = 0;
		foreach (var worldObjectGameData in chunkGameData.WorldObjectGameDataList)
		{
			if (worldObjectGameData is DoorObjectGameData)
			{
				doorCount++;
			}
		}
		SyncChunkData syncChunkData = new SyncChunkData
		{
			SyncChunkPosition = chunkGameData.ChunkPosition,
			
			SyncGroundTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.GroundTileGameDataList.Count),
			SyncFloorTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.FloorTileGameDataList.Count),
			SyncWallTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.WallTileGameDataList.Count),
			SyncOreTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.OreTileGameDataList.Count),
			SyncFoliageTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.FoliageTileGameDataList.Count),
			SyncLiquidTileDataList = new List<GenericGameObjectSyncData>(chunkGameData.LiquidTileGameDataList.Count),
			
			SyncObjectAssetDataList = new List<WorldObjectSyncData>(chunkGameData.WorldObjectGameDataList.Count),
			SyncDoorObjectDataList = new List<DoorObjectSyncData>(doorCount)
		};

		// Convert tile game data lists to agnostic sync data using helper
		ConvertTileList(chunkGameData.GroundTileGameDataList, syncChunkData.SyncGroundTileDataList);
		ConvertTileList(chunkGameData.FloorTileGameDataList, syncChunkData.SyncFloorTileDataList);
		ConvertTileList(chunkGameData.WallTileGameDataList, syncChunkData.SyncWallTileDataList);
		ConvertTileList(chunkGameData.OreTileGameDataList, syncChunkData.SyncOreTileDataList);
		ConvertTileList(chunkGameData.FoliageTileGameDataList, syncChunkData.SyncFoliageTileDataList);
		ConvertTileList(chunkGameData.LiquidTileGameDataList, syncChunkData.SyncLiquidTileDataList);

		// Convert world asset game data to agnostic sync data
		foreach (var worldObjectGameData in chunkGameData.WorldObjectGameDataList)
		{
			byte id = (byte)GameManager.Instance.GetIDFromWorldObject(worldObjectGameData.WO);
			
			if(worldObjectGameData is DoorObjectGameData doorObjectGameData)
			{
				syncChunkData.SyncDoorObjectDataList.Add(new DoorObjectSyncData(){ 
				Position = doorObjectGameData.Position, 
				ID = id, 
				Orientation = doorObjectGameData.Orientation, 
				IsOpen = doorObjectGameData.IsOpen});
			}
			else
			{
				syncChunkData.SyncObjectAssetDataList.Add(new WorldObjectSyncData(){ 
				Position = worldObjectGameData.Position, 
				ID = id, 
				Orientation = worldObjectGameData.Orientation});
			}
		}

		return syncChunkData;
	}

	// Helper function to convert tile lists to sync lists
	private void ConvertTileList(List<TileGameData> gameDataList, List<GenericGameObjectSyncData> syncDataList)
	{
		foreach (var tile in gameDataList)
		{
			byte id = GameManager.Instance.GetTileIdFromTileSO(tile.TileSO);
			syncDataList.Add(ConvertGameDataIntoGenericSyncData(tile.TilePosition, id));
		}
	}

	private GenericGameObjectSyncData ConvertGameDataIntoGenericSyncData(Vector2Int position, byte id)
	{
		return new GenericGameObjectSyncData
		{
			Position = position,
			ID = id
		};
	}
	
	private ChunkGameData ConvertToGameChunkData(SyncChunkData syncChunkData)
	{
		// Create a new ChunkGameData object
		ChunkGameData chunkGameData = new(ChunkManager.CHUNK_SIZE, syncChunkData.SyncChunkPosition);

		// Convert SyncGroundTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncGroundTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.GroundTileGameDataList);

		// Convert SyncFloorTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncFloorTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.FloorTileGameDataList);
		
		// Convert SyncWallTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncWallTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.WallTileGameDataList);

		// Convert SyncOreTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncOreTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.OreTileGameDataList);

		// Convert SyncFoliageTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncFoliageTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.FoliageTileGameDataList);

		// Convert SyncLiquidTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncLiquidTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.LiquidTileGameDataList);

		// Convert SyncWorldAssetData to WorldAssetGameData
		ConvertSyncDataList(syncChunkData.SyncObjectAssetDataList, (syncAsset) =>
		{
			WorldObject worldObject = GameManager.Instance.GetWorldObjectFromID(syncAsset.ID);
			return new WorldObjectGameData(worldObject, syncAsset.Position, syncAsset.Orientation);
		}, ref chunkGameData.WorldObjectGameDataList);
		
		// Convert SyncDoorObjectData To DoorObjectGameData
		ConvertSyncDataList(syncChunkData.SyncDoorObjectDataList, (syncDoor) => 
		{
			WorldObject worldObject = GameManager.Instance.GetWorldObjectFromID(syncDoor.ID);
			return new DoorObjectGameData(worldObject, syncDoor.Position, syncDoor.Orientation, syncDoor.IsOpen);
		}, ref chunkGameData.WorldObjectGameDataList);

		return chunkGameData;
	}

	// Generic helper function for converting Sync*DataList to *GameDataList
	private void ConvertSyncDataList<TSyncData, TGameData>(List<TSyncData> syncDataList, Func<TSyncData, TGameData> convertFunction, ref List<TGameData> gameDataList)
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