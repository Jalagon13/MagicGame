using UnityEngine;

[CreateAssetMenu(fileName = "New Wand", menuName = "Create Item/New Wand")]
public class WandItemSO : ItemSO
{
	[field: Tooltip("The number of spells that can be stored in the spell book.")]
	[field: SerializeField] public int Capacity { get; private set; } = 2;
	
	[field: Tooltip("Base Accuracy +- degrees of the spell casted by the wand.")]
	[field: SerializeField] public float Accuracy { get; private set; } = 3;
	
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