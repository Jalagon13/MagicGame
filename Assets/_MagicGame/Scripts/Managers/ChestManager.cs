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

	public void OpenChest(Vector2Int chestPosition, EnvironmentID playerEnvironment)
	{
		if (_forestChests.ContainsKey(chestPosition))
		{
			Debug.Log($"Chest open at position: {chestPosition}");
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

	public void CloseChest()
	{
		if (IsChestOpen)
		{
			Debug.Log($"Chest closed at position: {OpenChestPosition}");
			IsChestOpen = false;

			OnChestClose?.Invoke(this, EventArgs.Empty);
		}
		else
		{
			Debug.LogError($"Should not be trying to close a chest that is not open");
		}
	}

	public void TryToCreateEmptyChestData(Vector2Int chestPosition)
	{
		if (_forestChests.ContainsKey(chestPosition))
		{
			Debug.LogWarning($"A chest entry already exists for position: {chestPosition}");
			return;
		}

		// Create an entry for this position with an empty chest
		_forestChests.Add(chestPosition, new List<ChestItemData>());
		Debug.Log($"New empty chest entry added for position: {chestPosition}");
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
		
		if(openChestSlotItemData == null)
		{
			Debug.LogError("openCHestSLotItemData null. this message should never appear");
			return;
		}
		
		InventoryItem openChestSlotInventoryItem = new(GameManager.Instance.GetItemSOFromItemId(openChestSlotItemData.ItemId), openChestSlotItemData.Quantity);
		InventoryItem mouseItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;

		bool chestSlotHasItem = openChestSlotItemData != null;
		
		if(chestSlotHasItem)
		{
			if(mouseItem.HasItem) // Normal functionality
			{
				if(openChestSlotInventoryItem.Item.Name == mouseItem.Item.Name)
				{
					_forestChests[OpenChestPosition][clickedChestSlotIndex].Quantity += 1;
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
					
					_forestChests[OpenChestPosition][clickedChestSlotIndex].ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
					_forestChests[OpenChestPosition][clickedChestSlotIndex].Quantity = mouseItem.Quantity;
					
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = tempItem;
				}
			}
			else
			{
				int openChestSlotItemQuantity = openChestSlotInventoryItem.Quantity;
				int newChestSlotItemQuantity = openChestSlotItemQuantity / 2;
				int newMouseItemQuantity = openChestSlotItemQuantity - newChestSlotItemQuantity;
				
				_forestChests[OpenChestPosition][clickedChestSlotIndex].Quantity = newChestSlotItemQuantity;
				
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = openChestSlotInventoryItem.Item;
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity = newMouseItemQuantity;
				
				if(openChestSlotInventoryItem.Quantity == 0)
				{
					_forestChests[OpenChestPosition].Remove(openChestSlotItemData);
				}
				
				TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if(mouseItem.HasItem)
			{
				_forestChests[OpenChestPosition][clickedChestSlotIndex].ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
				_forestChests[OpenChestPosition][clickedChestSlotIndex].Quantity = 1;
				
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
	
	private ChestItemData FindChestItemData(int index)
	{
		foreach (ChestItemData chestItemData in _forestChests[OpenChestPosition])
		{
			if(chestItemData.SlotIndex == index)
			{
				// Found the chestSlot to work with
				return chestItemData;
			}
		}
		
		_forestChests[OpenChestPosition].Add(new ChestItemData
		{
			SlotIndex = index,
			ItemId = -1,
			Quantity = 0
		});
		
		foreach (ChestItemData chestItemData in _forestChests[OpenChestPosition])
		{
			if(chestItemData.SlotIndex == index)
			{
				// Found the chestSlot to work with
				return chestItemData;
			}
		}
		
		return null;
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
					FindChestItemData(clickedChestSlotIndex).Quantity += mouseItem.Quantity;
					// _forestChests[OpenChestPosition][clickedChestSlotIndex].Quantity += mouseItem.Quantity;
					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
					TooltipManager.Instance.Show(mouseItem is WandInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
				}
				else
				{
					// Swap the two items
					InventoryItem tempItem = openChestSlotInventoryItem;

					_forestChests[OpenChestPosition][clickedChestSlotIndex].ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
					_forestChests[OpenChestPosition][clickedChestSlotIndex].Quantity = mouseItem.Quantity;

					InventoryManager.Instance.GetMouseItem().MouseInventoryItem = tempItem;
				}
			}
			else
			{
				// If the mouse has no item, pick up the chest slot's item
				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = openChestSlotInventoryItem;
				_forestChests[OpenChestPosition][clickedChestSlotIndex] = new ChestItemData(); // Clear the chest slot
				TooltipManager.Instance.Hide();
			}
		}
		else
		{
			if (mouseItem.HasItem)
			{
				// If the chest slot is empty and the mouse has an item, place the item in the chest slot
				FindChestItemData(clickedChestSlotIndex).ItemId = GameManager.Instance.GetItemIdFromItemSO(mouseItem.Item);
				FindChestItemData(clickedChestSlotIndex).Quantity = mouseItem.Quantity;

				InventoryManager.Instance.GetMouseItem().MouseInventoryItem = new();
				TooltipManager.Instance.Show(mouseItem is WandInventoryItem wandItem ? wandItem.GetDescription() : mouseItem.Item.GetDescription(), mouseItem.Item.Name);
			}
		}

		// Update the inventory and play click feedbacks
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
		UpdateChestSlots();
	}
}