using UnityEngine;

[CreateAssetMenu(fileName = "New Spell Book", menuName = "Create Item/New Spell Book")]
public class SpellBookItemSO : ItemSO
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
		SpellBookInventoryItem spellBookInventoryItem = inventoryItem as SpellBookInventoryItem;
		SpellProjectileItemSO currentSpell = spellBookInventoryItem.GetCurrentSpell();

		if (currentSpell == null)
		{
			Debug.LogWarning("No valid spell to cast.");
			return RechargeTime; // Fallback in case there are no valid spells
		}

		// Calculate the total spread
		float calculatedSpread = Spread + currentSpell.Spread;
		if (calculatedSpread < 0)
		{
			calculatedSpread = 0; // Clamp spread to a minimum of 0
		}

		// Generate a random angle within the spread range
		float randomAngle = Random.Range(-calculatedSpread, calculatedSpread);
		Vector3 directionNormalized = ((Vector3)ActionManager.MouseWorldPosition - playerHand.ProjectileSpawnTransform.position).normalized;
		Vector3 rotatedDirection = Quaternion.Euler(0, 0, randomAngle) * directionNormalized;

		GameManager.Instance.SpawnSpellProjectile(Player.LocalClientInstance.CurrentBiome.Value, currentSpell, playerHand.ProjectileSpawnTransform.position, rotatedDirection, currentSpell.Speed, currentSpell.Damage, currentSpell.Lifetime);

		// Advance to the next spell and return the appropriate delay
		return spellBookInventoryItem.AdvanceToNextSpell(RechargeTime, CastDelay);
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new SpellBookInventoryItem(this, quantity, Capacity);
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}