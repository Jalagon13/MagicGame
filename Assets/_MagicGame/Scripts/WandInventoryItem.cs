using System;
using UnityEngine;

[Serializable]
public class WandInventoryItem : InventoryItem
{
	public event EventHandler OnWandContentsUpdated;
	
	[field: SerializeField] public SpellItemSO[] MagicArray { get; private set; }

	public WandInventoryItem(ItemSO itemSO, int quantity, int capacity) : base(itemSO, quantity)
	{
		MagicArray = new SpellItemSO[capacity];
	}
	
	public void ClearWandContentsUpdatedListeners()
	{
		OnWandContentsUpdated = null;
	}
	
	public void SetMagic(SpellItemSO magicItem, int magicIndex)
	{
		if(magicIndex < 0 || magicIndex >= MagicArray.Length)
		{
			Debug.LogError($"Magic index out of bounds");
			return;
		}
		
		MagicArray[magicIndex] = magicItem;
		OnWandContentsUpdated?.Invoke(this, EventArgs.Empty);
	}

	public SpellItemSO RemoveMagic(int magicIndex)
	{
		if(magicIndex < 0 || magicIndex >= MagicArray.Length)
		{
			Debug.LogError($"Magic index out of bounds");
			return null;
		}

		SpellItemSO removedMagic = MagicArray[magicIndex];
		MagicArray[magicIndex] = null;
		
		OnWandContentsUpdated?.Invoke(this, EventArgs.Empty);
		
		return removedMagic;
	}

	public SpellItemSO SwapMagic(SpellItemSO magic, int magicIndex)
	{
		if(magicIndex < 0 || magicIndex >= MagicArray.Length)
		{
			Debug.LogError($"Magic index out of bounds");
			return null;
		}

		SpellItemSO swappedMagic = MagicArray[magicIndex];
		MagicArray[magicIndex] = magic;
		
		OnWandContentsUpdated?.Invoke(this, EventArgs.Empty);
		
		return swappedMagic;
	}
}
