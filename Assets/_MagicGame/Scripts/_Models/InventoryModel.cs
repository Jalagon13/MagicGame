using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProjectWizard
{
	[Serializable]
	public class InventoryModel
	{
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
			// If item I want to add is stackable
			if(itemToAdd.Item.Stackable)
			{
				// Check if the item already exists in the inventory
				for(int i = 0; i < _inventoryItems.Count; i++)
				{
					if(!_inventoryItems[i].HasItem) continue; // If slot is empty, move on to the next slot to check
				
					if (_inventoryItems[i].Item.InGameName == itemToAdd.Item.InGameName)
					{
						_inventoryItems[i].Quantity += itemToAdd.Quantity;
					
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
	
		public void RemoveItem(ItemDataSO itemToRemove, int amountToRemove)
		{
			// Basic funationalty, need to revisit later to fix bugs
			for(int i = 0; i < _inventoryItems.Count; i++)
			{
				if(_inventoryItems[i].Item == null) continue;
			
				if(_inventoryItems[i].Item.InGameName == itemToRemove.InGameName)
				{
					_inventoryItems[i].Quantity -= amountToRemove;
				
					if(_inventoryItems[i].Quantity <= 0)
					{
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
			
				if(item.Item.InGameName == inventoryItemToCheck.Item.InGameName)
				{
					amountCounter += item.Quantity;
				}
			}
		
			return amountCounter >= inventoryItemToCheck.Quantity;
		}
	
		public int GetAmount(ItemDataSO itemToCheck)
		{
			int amountCounter = 0;
		
			foreach (InventoryItem item in _inventoryItems)
			{
				if(item.Item == null) continue;
			
				if(item.Item.InGameName == itemToCheck.InGameName)
				{
					amountCounter += item.Quantity;
				}
			}
		
			return amountCounter;
		}
	}

}