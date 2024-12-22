using System;
using System.Collections.Generic;
// using Pathfinding;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ChunkNetworkManager : NetworkBehaviour
{
	private int _chunkCountIterator;
	private int _chunksToLoadAmount;
	private List<Vector2Int> _playerChunksToLoadAroundPlayer;
	
	public void ClientUpdatePlayerChunks(EnvironmentID environmentToRequest)
	{
		// Debug.Log("Updating Chunks as Client");
		
		// Get chunks around the player the player wants to load
		_playerChunksToLoadAroundPlayer = ChunkManager.Instance.GetChunkPositionsToLoadAroundPlayer();
		_chunksToLoadAmount = _playerChunksToLoadAroundPlayer.Count;
		_chunkCountIterator = 0;
		
		// For each of those chunks, load them if they are not already loaded
		foreach (Vector2Int chunkPosition in _playerChunksToLoadAroundPlayer)
		{
			if(!ChunkManager.Instance.GetLoadedPlayerChunks().ContainsKey(chunkPosition))
			{
				RequestChunkDataServerRpc(environmentToRequest, chunkPosition);
			}
			else
			{
				_chunkCountIterator++;
			}
		}
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RequestChunkDataServerRpc(EnvironmentID environmentToRequest, Vector2Int chunkPosition, RpcParams rpcParams = default)
	{
		var chunkData = GetChunkGameData(environmentToRequest, chunkPosition);
		
		var syncChunkData = ConvertToSyncChunkData(chunkData);
		
		SendChunkDataToClientRpc(syncChunkData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void SendChunkDataToClientRpc(SyncChunkData syncChunkData, RpcParams rpcParams)
	{
		var chunkGameData = ConvertToGameChunkData(syncChunkData);
		
		ChunkManager.Instance.InvokeOnLoadChunk(chunkGameData);
		
		_chunkCountIterator++;
		if(_chunkCountIterator >= _chunksToLoadAmount)
		{
			// All loaded player chunks refreshed successfully
			// In the loaded player chunks, if any of them are not in playerChunksToLoadAroundPlayer, unload them
			List<Vector2Int> loadedChunkPositions = new(ChunkManager.Instance.GetLoadedPlayerChunks().Keys);
			foreach (Vector2Int loadedChunkPosition in loadedChunkPositions)
			{
				if (!_playerChunksToLoadAroundPlayer.Contains(loadedChunkPosition))
				{
					ChunkManager.Instance.InvokeOnUnloadChunk(ChunkManager.Instance.GetLoadedPlayerChunks()[loadedChunkPosition]);
				}
			}
			
			ChunkManager.Instance.InvokeOnLoadedPlayerChunksUpdated();
			_chunkCountIterator = 0;
		}
	}

	private ChunkGameData GetChunkGameData(EnvironmentID environmentToRequest, Vector2Int chunkPosition)
	{
		return ChunkManager.Instance.GetChunkDataFromChunkPosition(environmentToRequest, chunkPosition);
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
