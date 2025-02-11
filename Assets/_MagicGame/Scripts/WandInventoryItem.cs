using UnityEngine;

public class WandInventoryItem : InventoryItem
{
	public SpellItemSO[] SpellArray { get; private set; }

	public WandInventoryItem(ItemSO itemSO, int quantity, int capacity) : base(itemSO, quantity)
	{
		SpellArray = new SpellItemSO[capacity];
	}
	
	public void SetSpell(SpellItemSO spell, int spellIndex)
	{
		if(spellIndex < 0 || spellIndex >= SpellArray.Length)
		{
			Debug.LogError($"Spell index out of bounds");
			return;
		}
		
		SpellArray[spellIndex] = spell;
	}
	
	public SpellItemSO RemoveSpell(int spellIndex)
	{
		if(spellIndex < 0 || spellIndex >= SpellArray.Length)
		{
			Debug.LogError($"Spell index out of bounds");
			return null;
		}
		
		SpellItemSO removedSpell = SpellArray[spellIndex];
		SpellArray[spellIndex] = null;
		return removedSpell;
	}
	
	public SpellItemSO SwapSpells(SpellItemSO spell, int spellIndex)
	{
		if(spellIndex < 0 || spellIndex >= SpellArray.Length)
		{
			Debug.LogError($"Spell index out of bounds");
			return null;
		}
		
		SpellItemSO swappedSpell = SpellArray[spellIndex];
		SpellArray[spellIndex] = spell;
		
		return swappedSpell;
	}
}
