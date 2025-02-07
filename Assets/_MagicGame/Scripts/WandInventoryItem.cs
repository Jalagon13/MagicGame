using UnityEngine;

public class WandInventoryItem : InventoryItem
{
	public SpellProjectileItemSO[] SpellArray { get; private set; }

	public WandInventoryItem(ItemSO itemSO, int quantity, int capacity) : base(itemSO, quantity)
	{
		SpellArray = new SpellProjectileItemSO[capacity];
	}
	
	public void SetSpell(SpellProjectileItemSO spell, int spellIndex)
	{
		if(spellIndex < 0 || spellIndex >= SpellArray.Length)
		{
			Debug.LogError($"Spell index out of bounds");
			return;
		}
		
		SpellArray[spellIndex] = spell;
	}
	
	public SpellProjectileItemSO RemoveSpell(int spellIndex)
	{
		if(spellIndex < 0 || spellIndex >= SpellArray.Length)
		{
			Debug.LogError($"Spell index out of bounds");
			return null;
		}
		
		SpellProjectileItemSO removedSpell = SpellArray[spellIndex];
		SpellArray[spellIndex] = null;
		return removedSpell;
	}
	
	public SpellProjectileItemSO SwapSpells(SpellProjectileItemSO spell, int spellIndex)
	{
		if(spellIndex < 0 || spellIndex >= SpellArray.Length)
		{
			Debug.LogError($"Spell index out of bounds");
			return null;
		}
		
		SpellProjectileItemSO swappedSpell = SpellArray[spellIndex];
		SpellArray[spellIndex] = spell;
		
		return swappedSpell;
	}
}
