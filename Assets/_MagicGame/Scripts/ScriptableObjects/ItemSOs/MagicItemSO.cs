using UnityEngine;


public class MagicItemSO : ItemSO
{
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}
