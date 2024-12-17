using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseItemModel
{
	private InventoryItem _mouseInventoryItem;
	
	public InventoryItem MouseInventoryItem { get { return _mouseInventoryItem; } set { _mouseInventoryItem = value;}}
	
	public MouseItemModel()
	{
		_mouseInventoryItem = new();
	}
	
	public void OverrideMouseItem(InventoryItem item)
	{
		_mouseInventoryItem = item;
	}
	
	public void TryToRemoveItem(ItemSO itemToRemove, int amountToRemove, out int remainder)
	{
		if(_mouseInventoryItem.Item != null)
		{
			if(_mouseInventoryItem.Item.Name == itemToRemove.Name)
			{
				_mouseInventoryItem.Quantity -= amountToRemove;
				
				if(_mouseInventoryItem.Quantity <= 0)
				{
					// Note to future self: BUG: You are able to remove an amount of items even if it is greater than what it is in the stack. Need to fix this later
					
					_mouseInventoryItem = new();
				}
			}
		}
		
		remainder = amountToRemove - _mouseInventoryItem.Quantity;
	}
}
