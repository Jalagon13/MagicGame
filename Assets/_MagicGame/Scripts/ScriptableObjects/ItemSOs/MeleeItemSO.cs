using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "New Melee Item", menuName = "Create Item/New Melee Item")]
public class MeleeItemSO : ItemSO
{	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}
	
	public override string GetDescription()
	{
		StringBuilder description = new();
		
		description.Append(GetDescriptionBreak());
	
		return description.ToString();
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}
