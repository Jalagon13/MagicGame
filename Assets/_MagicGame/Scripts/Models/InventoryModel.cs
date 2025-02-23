using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryModel
{
	public event EventHandler<WandEventArgs> OnWandCollected;
	public event EventHandler<WandEventArgs> OnWandRemoved;
	public class WandEventArgs : EventArgs
	{
		public WandInventoryItem WandInvItem;
	}
	
	public event Action<List<InventoryItem>> OnInventoryUpdate;
	private List<InventoryItem> _inventoryItems = new();
	private int _slotAmount;

	public List<InventoryItem> InventoryItems { get { return _inventoryItems; } } 

	public InventoryModel(int slotAmount)
	{
		_slotAmount = slotAmount;

		for (int i = 0; i < _slotAmount; ++i)
		{
			_inventoryItems.Add(new InventoryItem() { Item = null, Quantity = 0 });
		}
	}
	
	public void UpdateInventory()
	{
		OnInventoryUpdate?.Invoke(_inventoryItems);
	}

	public void AddItem(InventoryItem itemToAdd)
	{
		Debug.Log($"Start of AddItem {itemToAdd.Item.Name} qnty: {itemToAdd.Quantity}, stackable {itemToAdd.Item.Stackable}");
	
		// If item I want to add is stackable
		if(itemToAdd.Item.Stackable)
		{
			// Check if the item already exists in the inventory
			for(int i = 0; i < _inventoryItems.Count; i++)
			{
				if(!_inventoryItems[i].HasItem) continue; // If slot is empty, move on to the next slot to check
				
				if (_inventoryItems[i].Item.Name == itemToAdd.Item.Name)
				{
					_inventoryItems[i].Quantity += itemToAdd.Quantity;
					Debug.Log($"THis shit NOT be playing");
					UpdateInventory();
					return;
				}
			}
			
			// If Item cannot be found in inventory, check for first empty slot
			for(int j = 0; j < _inventoryItems.Count; j++)
			{
				// If empty spot found, override this spot
				if (!_inventoryItems[j].HasItem)
				{
					// Override this slot with itemToAdd
					_inventoryItems[j] = itemToAdd;
					
					if(!_inventoryItems[j].Item.Stackable)
					{
						_inventoryItems[j].Quantity = 1;
					}
					
					UpdateInventory();
					return;
				}
			}
		}
		else // If item is not stackable
		{
			// Set itemToAdd quantity to 1 since all non-stackable items must be 1
			itemToAdd.Quantity = 1;

			// Loop through all slots
			for (int j = 0; j < _inventoryItems.Count; j++)
			{
				// If the slot is empty, override this spot
				if (!_inventoryItems[j].HasItem)
				{
					// Override this spot with itemToAdd
					_inventoryItems[j] = itemToAdd;

					if (!_inventoryItems[j].Item.Stackable)
					{
						_inventoryItems[j].Quantity = 1;
					}

					Debug.Log($"Adding to inv model index {j} {_inventoryItems[j].Item.Name} qnty {_inventoryItems[j].Quantity}");
					// If item being added was a wand, send this event
					if (_inventoryItems[j] is WandInventoryItem wandInvItem)
					{
						OnWandCollected?.Invoke(this, new WandEventArgs
						{
							WandInvItem = wandInvItem
						});
					}
					
					UpdateInventory();
					return;
				}
			}
		}

		// Inventory is full functionality (implement this later) 
		// (implement logic for adding unstackable items when inventory is full as well)
		// (Also impelement logic for wand functionality in this regard as well)
		UpdateInventory();
	}
	
	public void RemoveItem(ItemSO itemToRemove, int amountToRemove)
	{
		// Basic funationalty, need to revisit later to fix bugs
		for(int i = 0; i < _inventoryItems.Count; i++)
		{
			if(_inventoryItems[i].Item == null) continue;
			
			if(_inventoryItems[i].Item.Name == itemToRemove.Name)
			{
				_inventoryItems[i].Quantity -= amountToRemove;
				
				if(_inventoryItems[i].Quantity <= 0)
				{
					// Note to future self: BUG: You are able to remove an amount of items even if it is greater than what it is in the stack. Need to fix this later
					if(_inventoryItems[i] is WandInventoryItem wandInvItem)
					{
						OnWandRemoved?.Invoke(this, new WandEventArgs
						{
							WandInvItem = wandInvItem
						});
					}
					
					_inventoryItems[i] = new();
				}
				
				break;
			}
		}
		
		UpdateInventory();
	}
	
	public bool Contains(InventoryItem inventoryItemToCheck)
	{
		int amountCounter = 0;
		
		foreach (InventoryItem item in _inventoryItems)
		{
			if(item.Item == null) continue;
			
			if(item.Item.Name == inventoryItemToCheck.Item.Name)
			{
				amountCounter += item.Quantity;
			}
		}
		
		return amountCounter >= inventoryItemToCheck.Quantity;
	}
	
	public int GetAmount(ItemSO itemToCheck)
	{
		int amountCounter = 0;
		
		foreach (InventoryItem item in _inventoryItems)
		{
			if(item.Item == null) continue;
			
			if(item.Item.Name == itemToCheck.Name)
			{
				amountCounter += item.Quantity;
			}
		}
		
		return amountCounter;
	}
}
