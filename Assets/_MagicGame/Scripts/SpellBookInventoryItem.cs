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

	public void ResetSpellBook()
	{
		// Find the first valid spell index
		for (int i = 0; i < SpellsArray.Length; i++)
		{
			if (SpellsArray[i] != null)
			{
				_spellIndex = i - 1; // Set to one before the first valid spell, so the next cast starts here
				return;
			}
		}

		// If no valid spells, reset the index to -1
		_spellIndex = -1;
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

	public (float, SpellProjectileItemSO) CastSpell(float rechargeTime, float castDelay)
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

		// If there are no spells to cast, log a warning and return default values
		if (validSpellIndices.Count == 0)
		{
			Debug.LogWarning("No spells available to cast.");
			return (0f, null);
		}

		// Increment the spell index and wrap around when reaching the end
		_spellIndex = (_spellIndex + 1) % validSpellIndices.Count;

		// Get the current spell to cast
		int currentSpellIndex = validSpellIndices[_spellIndex];
		SpellProjectileItemSO currentSpell = SpellsArray[currentSpellIndex];

		// Return the cast delay or recharge time along with the current spell
		if (_spellIndex == validSpellIndices.Count - 1)
		{
			// If this is the last spell in the sequence, return recharge time
			return (rechargeTime, currentSpell);
		}
		else
		{
			// Otherwise, return the cast delay
			return (castDelay + currentSpell.CastDelay, currentSpell);
		}
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
