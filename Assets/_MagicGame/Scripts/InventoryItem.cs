using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// This class is the "manifestation" of the item that gets passed around in actual inventory slots
[Serializable]
public class InventoryItem
{
	public ItemSO Item;
	public int Quantity;
	public bool HasItem => Item != null;
	
	public InventoryItem(ItemSO itemSO, int quantity)
	{
		Item = itemSO;
		Quantity = quantity;
	}
	
	public InventoryItem()
	{
		Item = null;
		Quantity = 0;
	}
}

[Serializable]
public class SpriteContainer
{
	public Sprite spriteImage;
}