using UnityEngine;

[CreateAssetMenu(fileName = "New Spell Projectile", menuName = "Create Item/New Spell Projectile")]
public class SpellProjectileItemSO : ItemSO
{
	[field: Tooltip("Actual Prefab for the projectile.")]
	[field: SerializeField] public SpellProjectile SpellProjectilePrefab { get; private set; }

	[field: Tooltip("The mana cost required to cast this projectile.")]
	[field: SerializeField] public int ManaCost { get; private set; } = 5;

	[field: Tooltip("The amount of damage this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Damage { get; private set; } = 3;

	[field: Tooltip("The amount of randomness in the projectile's trajectory (in degrees). A higher value means more spread.")]
	[field: SerializeField] public float Spread { get; private set; } = 1f;
	
	[field: Tooltip("The lighttime in seconds of the projectile.")]
	[field: SerializeField] public float Lifetime { get; private set; } = 2f;

	[field: Tooltip("The speed at which the projectile travels.")]
	[field: SerializeField] public int Speed { get; private set; } = 100;

	[field: Tooltip("The additional delay (in seconds) added to the casting time of this projectile. Negative values reduce the delay.")]
	[field: SerializeField] public float CastDelay { get; private set; } = -0.03f;

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}
	
	public void SpawnAndShootSpell(Vector3 spawnPoint, Vector3 direction)
	{
		var spell = Instantiate(SpellProjectilePrefab, spawnPoint, Quaternion.identity);
		spell.Initialize(Speed, Damage, Lifetime, direction);
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