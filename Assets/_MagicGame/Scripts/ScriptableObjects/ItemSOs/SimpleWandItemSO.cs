using UnityEngine;

[CreateAssetMenu(fileName = "New Simple Wand Item", menuName = "Create Item/New Simple Wand Item")]
public class SimpleWandItemSO : ItemSO
{
	[SerializeField] private GameObject _placeDownProjectile;

	public override void ExecutePrimaryAction(InventoryItem inventoryItem)
	{
		if(inventoryItem is not SimpleWandInventoryItem || !(inventoryItem as SimpleWandInventoryItem).HasProjectile()) return;
		
		var simpleWandInventoryItem = inventoryItem as SimpleWandInventoryItem;
		var projectileItemSO = simpleWandInventoryItem.ProjectileItemSO;
		
		if(simpleWandInventoryItem.ProjectileQuantity <= 0) return;
		
		if(projectileItemSO is DeployItemSO || projectileItemSO is BuildItemSO)
		{
			GameObject projectile = Instantiate(_placeDownProjectile, Player.LocalClientInstance.GetWandProjectileSpawnPoint().position, Quaternion.identity);
			
			PlaceDownProjectile placeDownProjectile = projectile.GetComponent<PlaceDownProjectile>();
			
			Vector2 direction = ((Vector3)ActionManager.MouseWorldPosition - projectile.transform.position).normalized;
			
			placeDownProjectile.Initialize(projectileItemSO, direction);
			
			Player.LocalClientInstance.RemoveMana(1);
			simpleWandInventoryItem.RemoveProjectile();
		}
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
