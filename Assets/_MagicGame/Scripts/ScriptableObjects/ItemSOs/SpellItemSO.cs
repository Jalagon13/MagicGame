using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Create Item/New Spell")]
public class SpellItemSO : ItemSO
{
	[field: Tooltip("Actual Prefab for the projectile.")]
	[field: SerializeField] public Spell SpellProjectilePrefab { get; private set; }
	
	[field: Tooltip("The mana cost required to cast this projectile.")]
	[field: SerializeField] public int ManaCost { get; private set; } = 5;

	[field: Tooltip("The amount of damage this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Damage { get; private set; } = 3;
	
	[field: Tooltip("The amount of knockback this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Knockback { get; private set; } = 3;

	[field: Tooltip("The amount of randomness in the projectile's trajectory (in degrees). A higher value means more spread.")]
	[field: SerializeField] public float Spread { get; private set; } = 1f;
	
	[field: Tooltip("The lifetime in seconds of the projectile.")]
	[field: SerializeField] public float Lifetime { get; private set; } = 2f;

	[field: Tooltip("The speed at which the projectile travels.")]
	[field: SerializeField] public int Speed { get; private set; } = 100;

	[field: Tooltip("The additional delay (in seconds) added to the casting time of this projectile. Negative values reduce the delay.")]
	[field: SerializeField] public float CastDelay { get; private set; } = 0.1f;
	[field: SerializeField] public EventReference SpellCast { get; private set; }

	public void CastSpell(WandItemSO wandSO)
	{
		Vector2 baseDirection = (ActionManager.MouseWorldPosition - (Vector2)Player.LocalClientInstance.ProjectileSpawnPointTf.position).normalized;
		
		float spread = Spread + wandSO.Spread;
		float randomAngle = Random.Range(-spread, spread); 
		Vector2 spreadDirection = Quaternion.Euler(0, 0, randomAngle) * baseDirection; 
		
		BiomeType spawnBiome = Player.LocalClientInstance.CurrentBiome.Value;
		Vector2 spawnPos = Player.LocalClientInstance.ProjectileSpawnPointTf.position;
		
		GameManager.Instance.SpawnSpellProjectile(this, spawnBiome, spawnPos, spreadDirection, Speed, Damage, Lifetime, Knockback);
		SoundManager.Instance.PlayOneShot(SpellCast, spawnPos);
	}

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
