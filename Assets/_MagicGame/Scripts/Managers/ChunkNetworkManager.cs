using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChunkNetworkManager : NetworkBehaviour
{
	public void RequestChunkData(EnvironmentID environmentToRequest, Vector2Int chunkPosition)
	{
		RequestChunkDataServerRpc(environmentToRequest, chunkPosition);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RequestChunkDataServerRpc(EnvironmentID environmentToRequest, Vector2Int chunkPosition, RpcParams rpcParams = default)
	{
		var chunkData = ChunkManager.Instance.GetChunkData(environmentToRequest, chunkPosition);
		var syncChunkData = ConvertToSyncChunkData(chunkData);
		
		Pathfinding.Instance.AddPathfindingTiles(chunkPosition, chunkData, environmentToRequest);
		
		SendChunkDataToClientRpc(syncChunkData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void SendChunkDataToClientRpc(SyncChunkData syncChunkData, RpcParams rpcParams)
	{
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
			SyncWorldAssetDataList = new()
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

		// Convert world asset game data to agnostic sync data
		foreach (var asset in chunkGameData.WorldObjectGameDataList)
		{
			byte id = GameManager.Instance.GetByteIDFromWorldObject(asset.Asset);
			syncChunkData.SyncWorldAssetDataList.Add(ConvertGameDataIntoGenericSyncData(asset.Position, id));
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

		// Convert SyncWorldAssetData to WorldAssetGameData
		ConvertSyncDataList(syncChunkData.SyncWorldAssetDataList, (syncAsset) =>
		{
			WorldObject worldObject = GameManager.Instance.GetWorldObjectFromID(syncAsset.ID);
			return new WorldObjectGameData(worldObject, syncAsset.Position);
		}, ref chunkGameData.WorldObjectGameDataList);

		return chunkGameData;
	}

	// Generic helper function for converting Sync*DataList to *GameDataList
	private void ConvertSyncDataList<TSyncData, TGameData>(List<TSyncData> syncDataList, Func<TSyncData, TGameData> convertFunction, ref List<TGameData> gameDataList)
	{
		if (syncDataList == null)
		{
			gameDataList = new List<TGameData>();
		}
		else
		{
			gameDataList = new List<TGameData>();
			foreach (var syncData in syncDataList)
			{
				gameDataList.Add(convertFunction(syncData));
			}
		}
	}
}
