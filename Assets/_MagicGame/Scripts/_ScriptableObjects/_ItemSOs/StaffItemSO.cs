using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "staff_", menuName = "Create Item/New Staff")]
public class StaffItemSO : ItemSO
{
	// For custom spell modifiers
	[field: SerializeField] public int MeleeDamage { get; private set; }
	[field: SerializeField] public int Knockback { get; private set; }

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}
	
	public override string GetDescription()
	{
		return Description;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}