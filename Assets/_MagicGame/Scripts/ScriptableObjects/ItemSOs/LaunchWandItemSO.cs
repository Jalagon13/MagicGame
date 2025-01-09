using UnityEngine;

[CreateAssetMenu(fileName = "New Launch Wand Item", menuName = "Create Item/New Launch Wand Item")]
public class LaunchWandItemSO : ItemSO
{
	[SerializeField] private LaunchProjectileBehavior _launchProjectileBehaviorPrefab;
	[SerializeField] private int _baseDamage = 10;
	[SerializeField] private float _maxDistance = 20f; 
	[SerializeField] private float _speed = 10f; 
	[SerializeField] private LayerMask _collisionLayer; 
	[SerializeField] private float _collisionRadius = 0.1f; 
	[SerializeField] private float _rotationSpeed; 

	public override void ExecutePrimaryAction(InventoryItem inventoryItem)
	{
		if(inventoryItem is not SimpleWandInventoryItem || !(inventoryItem as SimpleWandInventoryItem).HasProjectile()) return;
		
		var simpleWandInventoryItem = inventoryItem as SimpleWandInventoryItem;
		var projectileItemSO = simpleWandInventoryItem.ProjectileItemSO;
		
		if(simpleWandInventoryItem.ProjectileQuantity <= 0) return;
		
		LaunchProjectileBehavior launchProjectile = Instantiate(_launchProjectileBehaviorPrefab, Player.LocalClientInstance.GetWandProjectileSpawnPoint().position, Quaternion.identity);
		
		Vector2 direction = ((Vector3)ActionManager.MouseWorldPosition - launchProjectile.transform.position).normalized;
		launchProjectile.Initialize(direction, _maxDistance, _speed, _collisionLayer, _collisionRadius, _rotationSpeed, projectileItemSO.ProjectileDamage + _baseDamage);
		
		if(projectileItemSO.ProjectileForm != null)
		{
			GameObject itemProjectileFormGameObject = Instantiate(projectileItemSO.ProjectileForm, Player.LocalClientInstance.GetWandProjectileSpawnPoint().position, Quaternion.identity);
			
			PlaceDownItemProjectileForm placeDownProjectileForm = itemProjectileFormGameObject.GetComponent<PlaceDownItemProjectileForm>();
			placeDownProjectileForm.Initialize(projectileItemSO, launchProjectile.transform);
		
			launchProjectile.OnProjectileCompleted += placeDownProjectileForm.OnProjectileCompleted;
			launchProjectile.OnProjectileNpcHit += placeDownProjectileForm.OnProjectileNpcHit;
		}
		else
		{
			// If there is no unique behavior found, just set the sprite of the projectile to the item sprite
			launchProjectile.SetProjectileBehaviorSprite(projectileItemSO.UiDisplay);
		}
		
		Player.LocalClientInstance.RemoveMana(1);
		simpleWandInventoryItem.RemoveProjectile();
	}

	public override void ExecuteSecondaryAction(InventoryItem inventoryItem)
	{

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
