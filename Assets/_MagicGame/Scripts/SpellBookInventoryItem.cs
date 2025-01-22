using UnityEngine;

public class SpellBookInventoryItem : InventoryItem
{
	public SpellProjectileItemSO[] SpellsArray { get; private set; }

	public SpellBookInventoryItem(ItemSO itemSO, int quantity, int capacity) : base(itemSO, quantity)
	{
		Item = itemSO;
		Quantity = quantity;
		SpellsArray = new SpellProjectileItemSO[capacity]; // Initialize the array with the given capacity
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

	public void SwapSpells(int firstIndex, int secondIndex)
	{
		if (firstIndex < 0 || firstIndex >= SpellsArray.Length || secondIndex < 0 || secondIndex >= SpellsArray.Length)
		{
			Debug.LogWarning("Invalid spell slot indices.");
			return;
		}

		SpellProjectileItemSO temp = SpellsArray[firstIndex];
		SpellsArray[firstIndex] = SpellsArray[secondIndex];
		SpellsArray[secondIndex] = temp;
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