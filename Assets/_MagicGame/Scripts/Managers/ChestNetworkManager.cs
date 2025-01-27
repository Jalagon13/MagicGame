using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct ChestSyncItemData : IEquatable<ChestSyncItemData>, INetworkSerializable
{
	public int SlotIndex;
	public int ItemId;
	public int Quantity;

	public bool Equals(ChestSyncItemData other)
	{
		return SlotIndex == other.SlotIndex && ItemId == other.ItemId && Quantity == other.Quantity;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref SlotIndex);
		serializer.SerializeValue(ref ItemId);
		serializer.SerializeValue(ref Quantity);
	}
}

public struct ChestSyncData : IEquatable<ChestSyncData>, INetworkSerializable
{
	public Vector2Int ChestPosition;
	public List<ChestSyncItemData> ChestItemData;

	public bool Equals(ChestSyncData other)
	{
		return ChestPosition == other.ChestPosition &&
			ChestItemData.Equals(other.ChestItemData);
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref ChestPosition);

		SerializeAgnosticDataList(serializer, ref ChestItemData);
	}

	private void SerializeAgnosticDataList<T>(BufferSerializer<T> serializer, ref List<ChestSyncItemData> chestItemList) where T : IReaderWriter
	{
		if (serializer.IsWriter)
		{
			// Serialize the list length
			ushort listLength = (ushort)chestItemList.Count;
			serializer.SerializeValue(ref listLength);

			// Serialize each tile in the list
			for (int i = 0; i < listLength; i++)
			{
				ChestSyncItemData syncTileData = chestItemList[i];
				serializer.SerializeValue(ref syncTileData);
			}
		}
		else
		{
			// Deserialize the list length first
			ushort listLength = 0;
			serializer.SerializeValue(ref listLength);

			// If the list length is 0, no further deserialization is needed
			if (listLength == 0)
			{
				return; // Skip deserialization if the list length is 0
			}

			// Initialize the list if it's null
			if (chestItemList == null)
			{
				chestItemList = new List<ChestSyncItemData>(listLength);
			}
			else
			{
				chestItemList.Clear(); // Clear the list if it's already initialized
			}

			// Deserialize each item in the list
			for (int i = 0; i < listLength; i++)
			{
				ChestSyncItemData chestSyncItemData = default;
				serializer.SerializeValue(ref chestSyncItemData);
				chestItemList.Add(chestSyncItemData);
			}
		}
	}
}

public class ChestNetworkManager : NetworkBehaviour
{
	public void OpenChestClient(Vector2Int chestPosition, EnvironmentID playerEnvironment)
	{
		RequestChestDataServerRpc(chestPosition, playerEnvironment);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RequestChestDataServerRpc(Vector2Int chestPosition, EnvironmentID environment, RpcParams rpcParams = default)
	{
		var chestData = ChestManager.Instance.GetChestDataFromEnvironment(environment);

		if (chestData.ContainsKey(chestPosition))
		{
			string chestId = $"{chestPosition}{environment}";
			if(!ChestManager.Instance.OpenedChestIds.Contains(chestId))
			{
				ChestManager.Instance.OpenedChestIds.Add(chestId);
			
				var syncData = new ChestSyncData
				{
					ChestItemData = ConvertToSyncChestData(chestData[chestPosition]),
					ChestPosition = chestPosition
				};
			
				SendChestDataClientRpc(syncData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
			}
		}
		else
		{
			Debug.LogError($"Chest not found for position: {chestPosition}.");
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void SendChestDataClientRpc(ChestSyncData syncData, RpcParams rpcParams)
	{
		// Convert the sync data back to game data and pass it to the ChestManager
		List<ChestItemData> chestItemData = ConvertToGameChestData(syncData.ChestItemData);
		ChestManager.Instance.OpenChest(syncData.ChestPosition, chestItemData);
	}

	private List<ChestSyncItemData> ConvertToSyncChestData(List<ChestItemData> chestItemDataToConvert)
	{
		List<ChestSyncItemData> syncChestData = new List<ChestSyncItemData>();

		if(chestItemDataToConvert != null && chestItemDataToConvert.Count > 0)
		{
			foreach (var chestItem in chestItemDataToConvert)
			{
				syncChestData.Add(new ChestSyncItemData
				{
					SlotIndex = chestItem.SlotIndex,
					ItemId = chestItem.ItemId,
					Quantity = chestItem.Quantity
				});
			}
		}

		return syncChestData;
	}

	private List<ChestItemData> ConvertToGameChestData(List<ChestSyncItemData> syncChestData)
	{
		List<ChestItemData> chestItemData = new List<ChestItemData>();

		if(syncChestData != null && syncChestData.Count > 0)
		{
			foreach (var syncItem in syncChestData)
			{
				chestItemData.Add(new ChestItemData
				{
					SlotIndex = syncItem.SlotIndex,
					ItemId = syncItem.ItemId,
					Quantity = syncItem.Quantity
				});
			}
		}

		return chestItemData;
	}

	public void RemoveChestId(Vector2Int openChestPosition, EnvironmentID value)
	{
		RemoveChestIdServerRpc(openChestPosition, value);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RemoveChestIdServerRpc(Vector2Int openChestPosition, EnvironmentID value)
	{
		Debug.Log($"Removing {openChestPosition}{value}");
		ChestManager.Instance.OpenedChestIds.Remove($"{openChestPosition}{value}");
	}

	public void UpdateChestContents(Vector2Int openChestPosition, EnvironmentID playerEnvironment, List<ChestItemData> localChestItemData)
	{
		var chestSyncData = new ChestSyncData
		{
			ChestItemData = ConvertToSyncChestData(localChestItemData),
			ChestPosition = openChestPosition
		};
	
		UpdateChestContentsServerRpc(openChestPosition, playerEnvironment, chestSyncData);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void UpdateChestContentsServerRpc(Vector2Int openChestPosition, EnvironmentID playerEnvironment, ChestSyncData syncChestItemData)
	{
		var chestGameData = ConvertToGameChestData(syncChestItemData.ChestItemData);
		
		var chestData = ChestManager.Instance.GetChestDataFromEnvironment(playerEnvironment);
		Debug.Log($"Dat shit updated from client");
		chestData[openChestPosition] = chestGameData;
	}
}