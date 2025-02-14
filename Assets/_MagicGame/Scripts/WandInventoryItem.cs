using System;
using UnityEngine;

public class WandInventoryItem : InventoryItem
{
	public event EventHandler OnWandContentsUpdated;

	public MagicItemSO[] MagicArray { get; private set; }

	public WandInventoryItem(ItemSO itemSO, int quantity, int capacity) : base(itemSO, quantity)
	{
		MagicArray = new MagicItemSO[capacity];
	}
	
	public void ClearWandContentsUpdatedListeners()
	{
		OnWandContentsUpdated = null;
	}
	
	public void SetMagic(MagicItemSO MagicItem, int magicIndex)
	{
		if(magicIndex < 0 || magicIndex >= MagicArray.Length)
		{
			Debug.LogError($"Magic index out of bounds");
			return;
		}
		
		MagicArray[magicIndex] = MagicItem;
		OnWandContentsUpdated?.Invoke(this, EventArgs.Empty);
	}
	
	public MagicItemSO RemoveMagic(int magicIndex)
	{
		if(magicIndex < 0 || magicIndex >= MagicArray.Length)
		{
			Debug.LogError($"Magic index out of bounds");
			return null;
		}
		
		MagicItemSO removedMagic = MagicArray[magicIndex];
		MagicArray[magicIndex] = null;
		
		OnWandContentsUpdated?.Invoke(this, EventArgs.Empty);
		
		return removedMagic;
	}
	
	public MagicItemSO SwapMagic(MagicItemSO magic, int magicIndex)
	{
		if(magicIndex < 0 || magicIndex >= MagicArray.Length)
		{
			Debug.LogError($"Magic index out of bounds");
			return null;
		}
		
		MagicItemSO swappedMagic = MagicArray[magicIndex];
		MagicArray[magicIndex] = magic;
		
		OnWandContentsUpdated?.Invoke(this, EventArgs.Empty);
		
		return swappedMagic;
	}
}
