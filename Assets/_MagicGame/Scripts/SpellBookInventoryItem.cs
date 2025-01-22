using UnityEngine;

public class SpellBookInventoryItem : InventoryItem
{
	public SpellProjectileItemSO[] SpellsArray { get; private set; }
	public int SpellIndex { get; private set; } = 0;
	
	public SpellBookInventoryItem(ItemSO itemSO, int quantity, int capacity) : base(itemSO, quantity)
	{
		Item = itemSO;
		Quantity = quantity;
		SpellsArray = new SpellProjectileItemSO[capacity]; // Initialize the array with the given capacity
	}
	
	public SpellProjectileItemSO GetSpellAndIncrementSpellIndex()
	{
		if (SpellsArray.Length == 0)
		{
			Debug.LogWarning("Spell array is empty.");
			return null;
		}

		SpellProjectileItemSO spell = SpellsArray[SpellIndex];
		SpellIndex = (SpellIndex + 1) % SpellsArray.Length; // Increment and wrap around
		return spell;
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
