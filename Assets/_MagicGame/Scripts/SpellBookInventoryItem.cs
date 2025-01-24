using System.Collections.Generic;
using UnityEngine;

public class SpellBookInventoryItem : InventoryItem
{
	public SpellProjectileItemSO[] SpellsArray { get; private set; }
	private int _spellIndex = -1;

	public SpellBookInventoryItem(ItemSO itemSO, int quantity, int capacity) : base(itemSO, quantity)
	{
		Item = itemSO;
		Quantity = quantity;
		SpellsArray = new SpellProjectileItemSO[capacity]; // Initialize the array with the given capacity
	}

	public (int, int) ResetSpellBook()
	{
		var spellBookItemSO = Item as SpellBookItemSO;

		// Find the first valid spell index
		for (int i = 0; i < SpellsArray.Length; i++)
		{
			if (SpellsArray[i] != null)
			{
				_spellIndex = i - 1; // Set to one before the first valid spell, so the next cast starts here
				return (spellBookItemSO.MaxMana, spellBookItemSO.ManaChargeSpeed);
			}
		}

		// If no valid spells, reset the index to -1
		_spellIndex = -1;

		return (spellBookItemSO.MaxMana, spellBookItemSO.ManaChargeSpeed);
	}

	public bool HasSpells()
	{
		for (int i = 0; i < SpellsArray.Length; i++)
		{
			if (SpellsArray[i] != null)
			{
				return true;
			}
		}

		return false;
	}
	
	public bool IsCurrentSpellFinalSpell()
	{
		// Collect valid spell indices (non-null) into a list
		List<int> validSpellIndices = new();
		for (int i = 0; i < SpellsArray.Length; i++)
		{
			if (SpellsArray[i] != null)
			{
				validSpellIndices.Add(i);
			}
		}

		// If no valid spells are available, return false
		if (validSpellIndices.Count == 0)
		{
			return false;
		}

		// Check if the current spell is the last valid spell
		int lastSpellIndex = validSpellIndices[validSpellIndices.Count - 1];
		return _spellIndex == lastSpellIndex;
	}

	public SpellProjectileItemSO GetCurrentSpell()
	{
		if (_spellIndex >= 0 && _spellIndex < SpellsArray.Length && SpellsArray[_spellIndex] != null)
		{
			return SpellsArray[_spellIndex];
		}

		// If the current index is invalid or null, find the first valid spell
		for (int i = 0; i < SpellsArray.Length; i++)
		{
			if (SpellsArray[i] != null)
			{
				_spellIndex = i; // Update the index to the first valid spell
				return SpellsArray[i];
			}
		}

		// If no valid spells exist, return null
		Debug.LogWarning($"Warning no valid spells exist, this warning should not be seen because check for empty spell book should already have been checked prior to this call");
		return null;
	}

	public float AdvanceToNextSpell(float rechargeTime, float spellBookCastDelay)
	{
		// Collect valid spells (non-null) into a list of indices
		List<int> validSpellIndices = new();
		for (int i = 0; i < SpellsArray.Length; i++)
		{
			if (SpellsArray[i] != null)
			{
				validSpellIndices.Add(i);
			}
		}

		// If there are no spells to cast, log a warning and return default recharge time
		if (validSpellIndices.Count == 0)
		{
			Debug.LogWarning("No spells available to cast.");
			return rechargeTime;
		}
		
		// Check if we are at the last spell in the sequence
		if (_spellIndex == validSpellIndices[validSpellIndices.Count - 1])
		{
			_spellIndex = (_spellIndex + 1) % validSpellIndices.Count;
			return rechargeTime;
		}

		// Otherwise, return the cast delay for the current spell
		float spellCastDelay = SpellsArray[validSpellIndices[_spellIndex]].CastDelay;
		
		_spellIndex = (_spellIndex + 1) % validSpellIndices.Count;
		
		return spellBookCastDelay + spellCastDelay;
	}

	public void SetSpell(int slotIndex, SpellProjectileItemSO spell)
	{
		if (slotIndex < 0 || slotIndex >= SpellsArray.Length)
		{
			Debug.LogWarning("Invalid spell slot index.");
			return;
		}

		SpellsArray[slotIndex] = spell;
	}

	public SpellProjectileItemSO RemoveSpell(int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= SpellsArray.Length)
		{
			Debug.LogWarning("Invalid spell slot index.");
			return null;
		}

		SpellProjectileItemSO removedSpell = SpellsArray[slotIndex];
		SpellsArray[slotIndex] = null; // Clear the slot
		return removedSpell; // Return the removed spell
	}

	public SpellProjectileItemSO SwapSpells(SpellProjectileItemSO spell, int slotIndex)
	{
		if (slotIndex < 0 || slotIndex >= SpellsArray.Length)
		{
			Debug.LogWarning("Invalid spell slot index.");
			return null;
		}

		SpellProjectileItemSO swappedSpell = SpellsArray[slotIndex]; // Store the spell currently in the slot
		SpellsArray[slotIndex] = spell; // Place the new spell in the slot
		return swappedSpell; // Return the swapped-out spell
	}

	public override string ToString()
	{
		string spellList = string.Empty;
		for (int i = 0; i < SpellsArray.Length; i++)
		{
			spellList += SpellsArray[i] != null ? SpellsArray[i].name : "Empty Slot";
			if (i < SpellsArray.Length - 1)
			{
				spellList += ", ";
			}
		}
		return $"SpellBookInventoryItem: [Spells: {spellList}]";
	}
}
