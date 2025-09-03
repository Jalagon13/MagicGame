using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct SyncItemData : IEquatable<SyncItemData>, INetworkSerializable
{
	public ushort ItemId;
	public int Quantity;
	public List<ushort> MagicArray;
	public int SelectedSpellIndex;

	public bool Equals(SyncItemData other)
	{
		// Check if basic properties are equal
		if (ItemId != other.ItemId || Quantity != other.Quantity || SelectedSpellIndex != other.SelectedSpellIndex)
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
			MagicArray = new List<ushort>(magicArrayCount);
		}

		// Serialize each item in the list
		for (int i = 0; i < magicArrayCount; i++)
		{
			int value = serializer.IsReader ? 0 : MagicArray[i];
			serializer.SerializeValue(ref value);

			if (serializer.IsReader)
			{
				MagicArray.Add((ushort)value);
			}
		}

		serializer.SerializeValue(ref SelectedSpellIndex);
	}
}

public struct ChestSyncData : IEquatable<ChestSyncData>, INetworkSerializable
{
	public Vector2Int ChestPosition;
	public List<SyncItemData> ChestItemData;

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

	private void SerializeAgnosticDataList<T>(BufferSerializer<T> serializer, ref List<SyncItemData> chestItemList) where T : IReaderWriter
	{
		if (serializer.IsWriter)
		{
			// Serialize the list length
			ushort listLength = (ushort)chestItemList.Count;
			serializer.SerializeValue(ref listLength);

			// Serialize each tile in the list
			for (int i = 0; i < listLength; i++)
			{
				SyncItemData syncTileData = chestItemList[i];
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
				chestItemList = new List<SyncItemData>(listLength);
			}
			else
			{
				chestItemList.Clear(); // Clear the list if it's already initialized
			}

			// Deserialize each item in the list
			for (int i = 0; i < listLength; i++)
			{
				SyncItemData chestSyncItemData = default;
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

	private List<SyncItemData> ConvertToSyncChestData(List<InventoryItem> chestItemDataToConvert)
	{
		List<SyncItemData> syncChestData = new List<SyncItemData>();

		Debug.Log(chestItemDataToConvert == null);
		foreach (InventoryItem invItem in chestItemDataToConvert)
		{
			List<ushort> magicArray = new();

			int selectedSpellIndex = -1;

			if (invItem is WandInventoryItem wandInventoryItem)
			{
				Debug.Log($"Found a wand to convert {wandInventoryItem.Item.InGameName}");
			
				for (int i = 0; i < wandInventoryItem.MagicArray.Length; i++)
				{
					magicArray.Add(wandInventoryItem.MagicArray[i] != null ? GameDataRegistry.Instance.GetItemIdFromItemData(wandInventoryItem.MagicArray[i]) : ushort.MaxValue);
				}

				selectedSpellIndex = wandInventoryItem.SelectedSpellIndex;
			}
			
			if(magicArray.Count > 0)
			{
				Debug.Log("Sending a magic array with something in it");
			}

			syncChestData.Add(new SyncItemData
			{
				ItemId = GameDataRegistry.Instance.GetItemIdFromItemData(invItem.Item),
				Quantity = invItem.Quantity,
				MagicArray = magicArray,
				SelectedSpellIndex = selectedSpellIndex
			});
		}

		return syncChestData;
	}

	private List<InventoryItem> ConvertToGameChestData(List<SyncItemData> syncChestData)
	{
		List<InventoryItem> chestItemData = new List<InventoryItem>();

		foreach (SyncItemData syncItem in syncChestData)
		{
			InventoryItem invItem = new(GameDataRegistry.Instance.GetItemDataFromItemId(syncItem.ItemId), syncItem.Quantity);
			
			if(invItem.Item is WandItemSO wandItemSO)
			{
				var wandInventoryItem = new WandInventoryItem(GameDataRegistry.Instance.GetItemDataFromItemId(syncItem.ItemId), syncItem.Quantity, wandItemSO.Capacity, syncItem.SelectedSpellIndex);

				Debug.Log($"Found a wand to turn to game data {invItem.Item.InGameName}");
			
				for (int i = 0; i < syncItem.MagicArray.Count; i++)
				{
					if(syncItem.MagicArray[i] >= 0)
					{
						wandInventoryItem.SetMagic(GameDataRegistry.Instance.GetItemDataFromItemId(syncItem.MagicArray[i]) as SpellItemSO, i);
					}
				}

				if(syncItem.SelectedSpellIndex >= 0 && syncItem.SelectedSpellIndex < wandItemSO.Capacity)
				{
					wandInventoryItem.SetSelectedSpellIndex(syncItem.SelectedSpellIndex);
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