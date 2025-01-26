using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class ChestItemData
{
	public int SlotIndex;
	public int ItemId;
	public int Quantity;
}

public class ChestManager : NetworkBehaviour
{
	public static ChestManager Instance { get; private set; }
	public event EventHandler OnChestClose;
	public event EventHandler<ChestEventArgs> OnChestOpen;
	public event EventHandler<ChestEventArgs> OnChestUpdated;
	public class ChestEventArgs : EventArgs
	{
		public List<ChestItemData> ChestItemData;
	}

	public bool IsChestOpen { get; private set; } = false;
	public Vector2Int OpenChestPosition { get; private set; }
	
	[SerializeField] private float _chestCloseDistance = 3f; 
	
	private Dictionary<Vector2Int, List<ChestItemData>> _forestChests = new();
	private Dictionary<Vector2Int, List<ChestItemData>> _caveChests = new();

	private void Awake()
	{
		Instance = this;
	}

	private void Update()
	{
		if (Player.LocalClientInstance == null || !IsChestOpen) return;

		var playerPosition = Player.LocalClientInstance.transform.position;
		var chestPosition = new Vector2(OpenChestPosition.x + 0.5f, OpenChestPosition.y + 0.5f);

		float distance = Vector2.Distance(playerPosition, chestPosition);

		if (distance > _chestCloseDistance)
		{
			CloseChest();
		}
	}
	
	public Dictionary<Vector2Int, List<ChestItemData>> GetChestDataFromEnvironment(EnvironmentID environment)
	{
		switch(environment)
		{
			case EnvironmentID.Forest:
				return _forestChests;
			case EnvironmentID.Cave:
				return _caveChests;
		}

		Debug.LogError($"Environment {environment} should exist but doesn't, add environment chunks to ChestManager");
		return null;
	}

	public void OpenChest(Vector2Int chestPosition, EnvironmentID playerEnvironment)
	{
		if (_forestChests.ContainsKey(chestPosition))
		{
			if(IsChestOpen == false)
			{
				InventoryManager.Instance.OnInventorySlotShiftLeftClicked += EnableChestShortcuts;
			}
		
			OpenChestPosition = chestPosition;
			IsChestOpen = true;

			OnChestOpen?.Invoke(this, new ChestEventArgs
			{
				ChestItemData = _forestChests[chestPosition]
			});
		}
		else
		{
			Debug.LogError($"Chest not found for position: {chestPosition}. This message should never play; chest data should always be found when opening.");
		}
	}

	private void EnableChestShortcuts(object sender, InventoryManager.ShortCutInventoryItemEventArgs e)
	{
		// NTFS: Shift Click chest functionality to be added here
	}

	public void CloseChest()
	{
		if (IsChestOpen)
		{
			InventoryManager.Instance.OnInventorySlotShiftLeftClicked -= EnableChestShortcuts;
			IsChestOpen = false;

			OnChestClose?.Invoke(this, EventArgs.Empty);
		}
	}
	
	public void AddChestEntry(Vector2Int chestPosition, List<ChestItemData> chestItems, EnvironmentID environment)
	{
		_forestChests.Add(chestPosition, chestItems);
	}

	public void CreateEmptyChestData(Vector2Int chestPosition)
	{
		if (_forestChests.ContainsKey(chestPosition))
		{
			Debug.LogWarning($"A chest entry already exists for position: {chestPosition}");
			return;
		}

		// Create an entry for this position with an empty chest
		_forestChests.Add(chestPosition, new List<ChestItemData>());
	}

	public void RemoveChestData(Vector2Int chestPosition)
	{
		if (_forestChests.ContainsKey(chestPosition))
		{
			_forestChests.Remove(chestPosition);
			Debug.Log($"Chest entry removed for position: {chestPosition}");
		}
	}
	
	private void UpdateChestSlots()
	{
		OnChestUpdated?.Invoke(this, new ChestEventArgs
		{
			ChestItemData = _forestChests[OpenChestPosition]
		});
	}
	
	public void ChestSlotRightClicked(int clickedChestSlotIndex)
	{
		// Define variables at the top, just like in ChestSlotRightClicked
		ChestItemData openChestSlotItemData = null;
		foreach (ChestItemData chestItemData in _forestChests[OpenChestPosition])
		{
			if(chestItemData.SlotIndex == clickedChestSlotIndex)
			{
				// Found the chestSlot to work with
				openChestSlotItemData = chestItemData;
			}
		}
		
		InventoryItem openChestSlotInventoryItem = openChestSlotItemData == null ? new() : new(GameManager.Instance.GetItemSOFromItemId(openChestSlotItemData.ItemId), openChestSlotItemData.Quantity);
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;

		bool chestSlotHasItem = openChestSlotItemData != null;

		if (chestSlotHasItem)
		{
			if(mouseItem.HasItem) // Normal functionality
			{
				if(openChestSlotInventoryItem.Item.Name == mouseItem.Item.Name)
				{
					GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).Quantity += 1;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity -= 1;
					
					if(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity <= 0)
					{
						InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
					}
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = openChestSlotInventoryItem;
					
					GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
					GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).Quantity = mouseItem.Quantity;
					
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = tempItem;
				}
			}
			else
			{
				int openChestSlotItemQuantity = openChestSlotInventoryItem.Quantity;
				int newChestSlotItemQuantity = openChestSlotItemQuantity / 2;
				int newMouseItemQuantity = openChestSlotItemQuantity - newChestSlotItemQuantity;
				
				GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).Quantity = newChestSlotItemQuantity;
				
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = openChestSlotInventoryItem.Item;
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = newMouseItemQuantity;
				
				if(GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).Quantity == 0)
				{
					RemoveChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex);
				}
				
				TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if(mouseItem.HasItem)
			{
				AddChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex, GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item), 1);
				
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity -= 1;
				if(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity <= 0)
				{
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
					TooltipManager.Instance.Show(mouseItem is WandInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
				}
			}
		}
		
		// Play click feedbacks and update Inventory
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
		UpdateChestSlots();
	}
	
	public void ChestSlotLeftClicked(int clickedChestSlotIndex)
	{
		// Define variables at the top, just like in ChestSlotRightClicked
		ChestItemData openChestSlotItemData = null;
		foreach (ChestItemData chestItemData in _forestChests[OpenChestPosition])
		{
			if(chestItemData.SlotIndex == clickedChestSlotIndex)
			{
				// Found the chestSlot to work with
				openChestSlotItemData = chestItemData;
			}
		}
		
		InventoryItem openChestSlotInventoryItem = openChestSlotItemData == null ? new() : new(GameManager.Instance.GetItemSOFromItemId(openChestSlotItemData.ItemId), openChestSlotItemData.Quantity);
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;

		bool chestSlotHasItem = openChestSlotItemData != null;

		if (chestSlotHasItem)
		{
			if (mouseItem.HasItem)
			{
				if (openChestSlotInventoryItem.Item.Name == mouseItem.Item.Name && mouseItem.Item.Stackable)
				{
					// If the items are the same and stackable, add the mouse item's quantity to the chest slot
					GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).Quantity += mouseItem.Quantity;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
					TooltipManager.Instance.Show(mouseItem is WandInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = openChestSlotInventoryItem;
					GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
					GetChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex).Quantity = mouseItem.Quantity;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = tempItem;
				}
			}
			else
			{
				// If the mouse has no item, pick up the chest slot's item
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = openChestSlotInventoryItem;
				RemoveChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex);
				TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if (mouseItem.HasItem)
			{
				// If the chest slot is empty and the mouse has an item, place the item in the chest slot
				Debug.Log($"Chest slot index clicked {clickedChestSlotIndex}");
				AddChestItemEntry(_forestChests[OpenChestPosition], clickedChestSlotIndex, GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item), mouseItem.Quantity);

				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
				TooltipManager.Instance.Show(mouseItem is WandInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
			}
		}

		// Update the inventory and play click feedbacks
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
		UpdateChestSlots();
	}
	
	private ChestItemData GetChestItemEntry(List<ChestItemData> chestItemDataContainer, int chestSlotIndexToGet)
	{
		foreach (ChestItemData chestItemData in chestItemDataContainer)
		{
			if(chestItemData.SlotIndex == chestSlotIndexToGet)
			{
				return chestItemData;
			}
		}
		
		Debug.LogWarning($"This chest does not have an entry to get at this chest slot index {chestSlotIndexToGet}");
		return null;
	}
	
	private void RemoveChestItemEntry(List<ChestItemData> chestItemDataContainer, int chestSlotIndex)
	{
		foreach (ChestItemData chestItemData in chestItemDataContainer)
		{
			if(chestItemData.SlotIndex == chestSlotIndex)
			{
				chestItemDataContainer.Remove(chestItemData);
				return;
			}
		}
	}
	
	private void AddChestItemEntry(List<ChestItemData> chestItemDataContainer, int chestSlotIndex, int itemId, int quantity)
	{
		chestItemDataContainer.Add(new()
		{
			SlotIndex = chestSlotIndex,
			ItemId = itemId,
			Quantity = quantity
		});
	}
}