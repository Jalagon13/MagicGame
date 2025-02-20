using UnityEngine;

[CreateAssetMenu(fileName = "New Wand", menuName = "Create Item/New Wand")]
public class WandItemSO : ItemSO
{
	[field: Tooltip("The delay (in seconds) between individual casts of the spell book.")]
	[field: SerializeField] public float BaseCastDelay { get; private set; } = 0.2f;
	
	[field: Tooltip("The Damage dealt when swung")]
	[field: SerializeField] public int BaseDamage { get; private set; } = 1;

	[field: Tooltip("The maximum amount of mana the spell book can hold.")]
	[field: SerializeField] public int MaxMana { get; private set; } = 150;

	[field: Tooltip("The rate at which mana is regenerated (mana per second).")]
	[field: SerializeField] public int ManaChargeSpeed { get; private set; } = 50;

	[field: Tooltip("The cooldown time (in seconds) before the spell book can be used again.")]
	[field: SerializeField] public float ReloadDuration { get; private set; } = 0.5f;

	[field: Tooltip("The amount of randomness in the trajectory of spells (in degrees). A higher value means more spread.")]
	[field: SerializeField] public float AccuracyAngle { get; private set; } = 0f;

	[field: Tooltip("The number of spells that can be stored in the spell book.")]
	[field: SerializeField] public int Capacity { get; private set; } = 2;

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