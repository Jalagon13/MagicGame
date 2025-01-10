using UnityEngine;

[CreateAssetMenu(fileName = "New Launch Wand Item", menuName = "Create Item/New Launch Wand Item")]
public class LaunchWandItemSO : ItemSO
{
	[SerializeField] private LaunchWandProjectile _launchProjectileBehaviorPrefab;
	[SerializeField] private LayerMask _collisionLayer; 
	[SerializeField] private float _distanceModifierPercent = 10f; 
	[SerializeField] private float _speedModifierPercent = 10f; 
	[SerializeField] private int _wandDamage = 2;

	public override float ExecutePrimaryAction(InventoryItem inventoryItem)
	{
		if(inventoryItem is not SimpleWandInventoryItem || !(inventoryItem as SimpleWandInventoryItem).HasProjectile()) return 0.1f;
		
		var projectileItemSO = (inventoryItem as SimpleWandInventoryItem).ProjectileItemSO;
		
		LaunchWandProjectile launchProjectile = Instantiate(_launchProjectileBehaviorPrefab, Player.LocalClientInstance.GetWandProjectileSpawnPoint().position, Quaternion.identity);
		
		Vector2 direction = ((Vector3)ActionManager.MouseWorldPosition - launchProjectile.transform.position).normalized;
		
		launchProjectile.Initialize(direction, 
		projectileItemSO.BaseDistance * (1 + (_distanceModifierPercent * 0.01f)), 
		projectileItemSO.BaseSpeed * (1 + (_speedModifierPercent * 0.01f)), 
		_collisionLayer, 
		projectileItemSO.RotationSpeedDegreesPerSecond, 
		projectileItemSO.BaseDamage + _wandDamage, 
		projectileItemSO.UiDisplay);
		
		Player.LocalClientInstance.RemoveMana(projectileItemSO.ManaCost);
		
		if(projectileItemSO.CustomBehaviorPrefab != null)
		{
			// If projectile has any custom behaviors, Instantiate it and attach it to as a child to the main projectile
			var customItemBehavior = Instantiate(projectileItemSO.CustomBehaviorPrefab, default, Quaternion.identity);
			customItemBehavior.transform.SetParent(launchProjectile.transform);
		}
		
		return projectileItemSO.CastCooldown;
	}

	public override float ExecuteSecondaryAction(InventoryItem inventoryItem)
	{
		return _baseActionCooldown;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new SimpleWandInventoryItem(this, quantity);
	}

	public override string GetDescription()
	{
		return string.Empty;
	}
}
