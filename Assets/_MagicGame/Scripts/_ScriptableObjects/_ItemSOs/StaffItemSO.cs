using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "staff_", menuName = "Create Item/New Staff")]
public class StaffItemSO : ItemSO
{
	[Tooltip("Power of each mining tick")]
	[field: SerializeField] public int MiningPower { get; private set; }
	[Tooltip("Mining speed / 60 = time between mining ticks")]
	[field: SerializeField] public float MiningRange { get; private set; }
	[field: SerializeField] public MiningVisuals MiningVisualsPrefab { get; private set; }

	
	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}
	
	public bool PlayerWithinMiningRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
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