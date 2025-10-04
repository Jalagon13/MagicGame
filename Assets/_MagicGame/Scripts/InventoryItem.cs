using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;


namespace ProjectWizard
{
	// This class is the "manifestation" of the item that gets passed around in actual inventory slots
	[Serializable]
	public class InventoryItem
	{
		public ItemDataSO Item;
		public int Quantity;
		public bool HasItem => Item != null;
		public ulong Id { get; private set; }
	
		public InventoryItem(ItemDataSO itemSO, int quantity)
		{
			Item = itemSO;
		
			if(Item != null)
			{
				Quantity = quantity;
				Id = IdGenerator.GenerateRandomId();
			}
		}
	
		public void SetId(ulong newId)
		{
			Id = newId;
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
}