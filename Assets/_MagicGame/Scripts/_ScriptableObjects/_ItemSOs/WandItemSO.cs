using UnityEngine;

[CreateAssetMenu(fileName = "New Wand", menuName = "Create Item/New Wand")]
public class WandItemSO : ItemSO
{
	[field: Tooltip("The number of spells that can be stored in the spell book.")]
	[field: SerializeField] public int Capacity { get; private set; } = 2;
	[field: Tooltip("Duration after spell sequence is cast before the next sequence can be cast.")]
	[field: SerializeField] public float RechargeTime { get; private set; } = 2;

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new WandInventoryItem(this, quantity, Capacity);
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}