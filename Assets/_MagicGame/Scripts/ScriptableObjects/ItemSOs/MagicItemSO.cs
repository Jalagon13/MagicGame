using System.Text;
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
		StringBuilder description = new();
		description.Append($"Can be placed in a wand slot<br>");
		description.Append($"{GetDescriptionBreak()}");

		return description.ToString();
	}
}
