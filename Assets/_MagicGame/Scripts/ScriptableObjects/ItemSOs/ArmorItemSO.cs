using UnityEngine;

public enum ArmorType { Head, Chest, Legs }

[CreateAssetMenu(fileName = "New Armor", menuName = "Create Item/New Armor")]
public class ArmorItemSO : ItemSO
{
	[field: SerializeField] public ArmorType ArmorType { get; private set; }
	[field: SerializeField] public int DefenseAmount { get; private set; }

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}
