using UnityEngine;

public class SimpleWandInventoryItem : InventoryItem
{
	public ItemSO ProjectileItemSO { get; private set; }

	public SimpleWandInventoryItem(ItemSO itemSO, int quantity) : base(itemSO, quantity)
	{
		// Add custom behavior or properties here for wands
	}
	
	// Sets the item to be shot by this wand
	public void SetProjectileItemSO(ItemSO itemSO)
	{
		ProjectileItemSO = itemSO;
	}
}
