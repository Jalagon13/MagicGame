using UnityEngine;

[CreateAssetMenu(fileName = "New SpellBook", menuName = "Create Item/New SpellBook")]
public class SpellBookItemSO : ItemSO
{
	[field: Tooltip("The number of spells that can be stored in the spell book.")]
	[field: SerializeField] public int Capacity { get; private set; } = 2;

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new SpellbookInventoryItem(this, quantity, Capacity);
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}