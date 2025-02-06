using UnityEngine;

[CreateAssetMenu(fileName = "New Wand", menuName = "Create Item/New Wand")]
public class WandItemSO : ItemSO
{
	[field: Tooltip("The number of spells cast simultaneously per use of the spell book.")]
	[field: SerializeField] public int SpellsCast { get; private set; } = 1;

	[field: Tooltip("The delay (in seconds) between individual casts of the spell book.")]
	[field: SerializeField] public float CastDelay { get; private set; } = 0.2f;

	[field: Tooltip("The cooldown time (in seconds) before the spell book can be used again.")]
	[field: SerializeField] public float RechargeTime { get; private set; } = 0.5f;

	[field: Tooltip("The maximum amount of mana the spell book can hold.")]
	[field: SerializeField] public int MaxMana { get; private set; } = 150;

	[field: Tooltip("The rate at which mana is regenerated (mana per second).")]
	[field: SerializeField] public int ManaChargeSpeed { get; private set; } = 50;

	[field: Tooltip("The number of spells that can be stored in the spell book.")]
	[field: SerializeField] public int Capacity { get; private set; } = 2;

	[field: Tooltip("The amount of randomness in the trajectory of spells (in degrees). A higher value means more spread.")]
	[field: SerializeField] public float Spread { get; private set; } = 0f;

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}