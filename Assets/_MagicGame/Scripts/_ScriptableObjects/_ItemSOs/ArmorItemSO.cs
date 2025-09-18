using System.Text;
using UnityEngine;

public enum ArmorType { Head, Chest, Legs }

[CreateAssetMenu(fileName = "New Armor", menuName = "Create Item/New Armor")]
public class ArmorItemSO : ItemDataSO
{
	[field: SerializeField] 
	public ArmorType ArmorType { get; private set; }
	
	[field: SerializeField] 
	public int DefenseAmount { get; private set; }
	
	[field: SerializeField] 
	public ArmorSpritesSO ArmorSprites { get; private set; }
	
	[field: SerializeField, Tooltip("If true, this armor will be rendered on top of default sprite, if false, it will replace the default sprite.")]
	public bool OverlayArmor { get; private set; }

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}

	public override string GetDescription()
	{
		StringBuilder description = new();
		description.Append($"Can be placed in equipment slot<br>");
		description.Append($"Defense: {DefenseAmount}<br>");
		description.Append($"{GetDescriptionBreak()}");

		return description.ToString();
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}
