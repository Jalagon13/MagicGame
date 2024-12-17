using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryModel
{
    public event Action<List<InventoryItem>> OnInventoryUpdate;
    private List<InventoryItem> _inventoryItems = new();
    private InventoryItem _mouseItem;
    private int _slotAmount;

    public List<InventoryItem> InventoryItems => _inventoryItems;

    public InventoryModel(int slotAmount, InventoryItem mouseItem)
    {
        _slotAmount = slotAmount;
        _mouseItem = mouseItem;

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
				
                if (_inventoryItems[i].Item.Name == itemToAdd.Item.Name)
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
                    UpdateInventory();
                    return;
                }
            }
        }
        else // If item is not stackable
        {
            // Grab quantity of item before I set it to 1
            int amountToIterate = itemToAdd.Quantity;
			
            // Set itemToAdd quantity to 1 for calculation
            itemToAdd.Quantity = 1;
			
            // For each iteration of the quantity of this unstackable item,
            for (int i = 0; i < amountToIterate; i++)
            {
                // If itemToAdd isn't a WandInventoryObject and itemToAdd.Item is a WandObject, 
                if (itemToAdd is not WandInventoryItem && itemToAdd.Item is WandItemSO)
                {
                    // Create itemToAdd as a WandInventoryObject and process it below
                    itemToAdd = new WandInventoryItem(itemToAdd.Item, 1);
                }
			
                // Loop through all slots
                for(int j = 0; j < _inventoryItems.Count; j++)
                {
                    // If the slot is empty, override this spot
                    if(!_inventoryItems[j].HasItem)
                    {
                        // Override this spot with itemToAdd
                        _inventoryItems[j] = itemToAdd;
                        break;
                    }
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
