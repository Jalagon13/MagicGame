using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChunkNetworkManager : NetworkBehaviour
{
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void RequestChunkDataServerRpc(ulong clientId, BiomeType requestBiome, Vector2Int chunkPosition, RpcParams rpcParams = default)
	{
		var chunkData = ChunkManager.Instance.GetChunkFromChunkPosition(requestBiome, chunkPosition);
		var syncChunkData = ConvertToSyncChunkData(chunkData);
		
		Pathfinding.Instance.UpdateChunkPathfinding(chunkPosition, chunkData, requestBiome, clientId);
		
		SendChunkDataToClientRpc(requestBiome, syncChunkData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void SendChunkDataToClientRpc(BiomeType requestBiome, SyncChunkData syncChunkData, RpcParams rpcParams)
	{
		if(Player.LocalClientInstance.CurrentPlayerBiome.Value != requestBiome) return;
	
		var chunkGameData = ConvertToGameChunkData(syncChunkData);
		ChunkManager.Instance.InvokeOnLoadChunk(chunkGameData);
	}
	
	private SyncChunkData ConvertToSyncChunkData(ChunkGameData chunkGameData)
	{
		// Create a new SyncChunkData object
		SyncChunkData syncChunkData = new SyncChunkData
		{
			SyncChunkPosition = chunkGameData.ChunkPosition,
			SyncGroundTileDataList = new(),
			SyncWallTileDataList = new(),
			SyncFloorTileDataList = new(),
			SyncObjectAssetDataList = new(),
			SyncDoorObjectDataList = new()
		};

		// Convert ground tile game data to agnostic sync data
		foreach (var tile in chunkGameData.GroundTileGameDataList)
		{
			byte id = GameManager.Instance.GetTileIdFromTileSO(tile.TileSO);
			syncChunkData.SyncGroundTileDataList.Add(ConvertGameDataIntoGenericSyncData(tile.TilePosition, id));
		}

		// Convert wall tile game data to agnostic sync data
		foreach (var tile in chunkGameData.WallTileGameDataList)
		{
			byte id = GameManager.Instance.GetTileIdFromTileSO(tile.TileSO);
			syncChunkData.SyncWallTileDataList.Add(ConvertGameDataIntoGenericSyncData(tile.TilePosition, id));
		}
		
		// Convert floor tile game data to agnostic sync data
		foreach (var tile in chunkGameData.FloorTileGameDataList)
		{
			byte id = GameManager.Instance.GetTileIdFromTileSO(tile.TileSO);
			syncChunkData.SyncFloorTileDataList.Add(ConvertGameDataIntoGenericSyncData(tile.TilePosition, id));
		}

		// Convert world asset game data to agnostic sync data
		foreach (var worldObjectGameData in chunkGameData.WorldObjectGameDataList)
		{
			byte id = (byte)GameManager.Instance.GetIDFromWorldObject(worldObjectGameData.WO);
			
			if(worldObjectGameData is DoorObjectGameData doorObjectGameData)
			{
				syncChunkData.SyncDoorObjectDataList.Add(new DoorObjectSyncData(){ Position = doorObjectGameData.Position, ID = id, IsOpen = doorObjectGameData.IsOpen});
			}
			else
			{
				syncChunkData.SyncObjectAssetDataList.Add(ConvertGameDataIntoGenericSyncData(worldObjectGameData.Position, id));
			}
		}

		return syncChunkData;
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

		// Convert SyncWallTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncWallTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.WallTileGameDataList);
		
		// Convert SyncFloorTileData to TileGameData
		ConvertSyncDataList(syncChunkData.SyncFloorTileDataList, (syncTile) =>
		{
			TileSO tileSO = GameManager.Instance.GetTileSOFromID(syncTile.ID);
			return new TileGameData(tileSO, new(syncTile.Position.x, syncTile.Position.y));
		}, ref chunkGameData.FloorTileGameDataList);

		// Convert SyncWorldAssetData to WorldAssetGameData
		ConvertSyncDataList(syncChunkData.SyncObjectAssetDataList, (syncAsset) =>
		{
			WorldObject worldObject = GameManager.Instance.GetWorldObjectFromID(syncAsset.ID);
			return new WorldObjectGameData(worldObject, syncAsset.Position);
		}, ref chunkGameData.WorldObjectGameDataList);
		
		// Convert SyncDoorObjectData To DoorObjectGameData
		ConvertSyncDataList(syncChunkData.SyncDoorObjectDataList, (syncDoor) => 
		{
			WorldObject worldObject = GameManager.Instance.GetWorldObjectFromID(syncDoor.ID);
			return new DoorObjectGameData(worldObject, syncDoor.Position, syncDoor.IsOpen);
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
