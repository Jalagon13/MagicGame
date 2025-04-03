using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct ChestSyncItemData : IEquatable<ChestSyncItemData>, INetworkSerializable
{
	public int ItemId;
	public int Quantity;
	public List<int> MagicArray;

	public bool Equals(ChestSyncItemData other)
	{
		// Check if basic properties are equal
		if (ItemId != other.ItemId || Quantity != other.Quantity)
			return false;

		// Check if both MagicArrays are either null or have the same count
		if (MagicArray == null && other.MagicArray == null)
			return true;
		if (MagicArray == null || other.MagicArray == null || MagicArray.Count != other.MagicArray.Count)
			return false;

		// Compare each element in the lists
		for (int i = 0; i < MagicArray.Count; i++)
		{
			if (MagicArray[i] != other.MagicArray[i])
				return false;
		}

		return true;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref ItemId);
		serializer.SerializeValue(ref Quantity);

		// Serialize the count of the list first
		int magicArrayCount = MagicArray != null ? MagicArray.Count : 0;
		serializer.SerializeValue(ref magicArrayCount);

		// Initialize the list if deserializing
		if (serializer.IsReader && MagicArray == null)
		{
			MagicArray = new List<int>(magicArrayCount);
		}

		// Serialize each item in the list
		for (int i = 0; i < magicArrayCount; i++)
		{
			int value = serializer.IsReader ? 0 : MagicArray[i];
			serializer.SerializeValue(ref value);

			if (serializer.IsReader)
			{
				MagicArray.Add(value);
			}
		}
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
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void RequestChestDataServerRpc(Vector2Int chestPosition, BiomeType biome, RpcParams rpcParams = default)
	{
		var biomeChestData = ChestManager.Instance.GetChestDataFromBiome(biome);

		if (biomeChestData.ContainsKey(chestPosition))
		{
			string chestId = $"{chestPosition}{biome}";
			var syncData = new ChestSyncData
			{
				ChestItemData = ConvertToSyncChestData(biomeChestData[chestPosition]),
				ChestPosition = chestPosition
			};

			SendChestDataClientRpc(syncData, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
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
		List<InventoryItem> chestItemData = ConvertToGameChestData(syncData.ChestItemData);
		ChestManager.Instance.OnChestDataRecieved(syncData.ChestPosition, chestItemData);
	}

	private List<ChestSyncItemData> ConvertToSyncChestData(List<InventoryItem> chestItemDataToConvert)
	{
		List<ChestSyncItemData> syncChestData = new List<ChestSyncItemData>();

		Debug.Log(chestItemDataToConvert == null);
		foreach (InventoryItem invItem in chestItemDataToConvert)
		{
			List<int> magicArray = new();

			if (invItem is WandInventoryItem wandInventoryItem)
			{
				Debug.Log($"Found a wand to convert {wandInventoryItem.Item.Name}");
			
				for (int i = 0; i < wandInventoryItem.MagicArray.Length; i++)
				{
					magicArray.Add(wandInventoryItem.MagicArray[i] != null ? GameManager.Instance.GetItemIdFromItemSO(wandInventoryItem.MagicArray[i]) : -1);
				}
			}
			
			if(magicArray.Count > 0)
			{
				Debug.Log("Sending a magic array with something in it");
			}

			syncChestData.Add(new ChestSyncItemData
			{
				ItemId = GameManager.Instance.GetItemIdFromItemSO(invItem.Item),
				Quantity = invItem.Quantity,
				MagicArray = magicArray
			});
		}

		return syncChestData;
	}

	private List<InventoryItem> ConvertToGameChestData(List<ChestSyncItemData> syncChestData)
	{
		List<InventoryItem> chestItemData = new List<InventoryItem>();

		foreach (ChestSyncItemData syncItem in syncChestData)
		{
			InventoryItem invItem = new(GameManager.Instance.GetItemSOFromItemId(syncItem.ItemId), syncItem.Quantity);
			
			if(invItem.Item is WandItemSO wandItemSO)
			{
				var wandInventoryItem = new WandInventoryItem(GameManager.Instance.GetItemSOFromItemId(syncItem.ItemId), syncItem.Quantity, wandItemSO.Capacity);
			
				Debug.Log($"Found a wand to turn to game data {invItem.Item.Name}");
			
				for (int i = 0; i < syncItem.MagicArray.Count; i++)
				{
					if(syncItem.MagicArray[i] > -1)
					{
						wandInventoryItem.SetMagic(GameManager.Instance.GetItemSOFromItemId(syncItem.MagicArray[i]) as MagicItemSO, i);
					}
				}

				chestItemData.Add(wandInventoryItem);
			}
			else
			{
				chestItemData.Add(invItem);
			}
		}

		return chestItemData;
	}
	
	public void UpdateChestContents(Vector2Int openChestPosition, BiomeType playerBiome, List<InventoryItem> localChestItemData)
	{
		Debug.Log(localChestItemData == null);
		var chestSyncData = new ChestSyncData
		{
			ChestItemData = ConvertToSyncChestData(localChestItemData),
			ChestPosition = openChestPosition
		};
	
		UpdateChestContentsServerRpc(openChestPosition, playerBiome, chestSyncData);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void UpdateChestContentsServerRpc(Vector2Int openChestPosition, BiomeType playerBiome, ChestSyncData syncChestItemData)
	{
		var chestData = ChestManager.Instance.GetChestDataFromBiome(playerBiome);
		chestData[openChestPosition] = ConvertToGameChestData(syncChestItemData.ChestItemData);
	}
}