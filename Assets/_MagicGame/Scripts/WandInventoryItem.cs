using System;
using UnityEngine;

[Serializable]
public class WandInventoryItem : InventoryItem
{
	public event EventHandler OnWandContentsUpdated;
	
	[field: SerializeField] 
	public SpellItemSO[] MagicArray { get; private set; }
	
	[HideInInspector]
	public int SelectedSpellIndex { get; private set; }

	public WandInventoryItem(ItemDataSO itemSO, int quantity, int capacity, int selectedSpellIndex) : base(itemSO, quantity)
	{
		MagicArray = new SpellItemSO[capacity];
		SelectedSpellIndex = selectedSpellIndex;
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
	
	public void SetSelectedSpellIndex(int index)
	{
		if (index < 0 || index >= MagicArray.Length)
		{
			Debug.LogError($"Selected spell index out of bounds: {index}");
			return;
		}

		SelectedSpellIndex = index;
	}
	
	public bool HasSpells()
	{
		foreach (var spell in MagicArray)
		{
			if (spell != null)
			{
				return true;
			}
		}
		return false;
	}
	
	public SpellItemSO GetSelectedSpell()
	{
		if (SelectedSpellIndex < 0 || SelectedSpellIndex >= MagicArray.Length)
		{
			Debug.LogError($"Selected spell index out of bounds: {SelectedSpellIndex}");
			return null;
		}

		return MagicArray[SelectedSpellIndex];
	}
}
