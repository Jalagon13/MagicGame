using UnityEngine;

[CreateAssetMenu(fileName = "New Simple Wand Item", menuName = "Create Item/New Simple Wand Item")]
public class SimpleWandItemSO : ItemSO
{
	[SerializeField] private GameObject _simpleProjectilePrefab;

	public override void ExecutePrimaryAction(InventoryItem inventoryItem)
	{
		var simpleWandInventoryItem = inventoryItem as SimpleWandInventoryItem;
		
		GameObject projectile = Instantiate(_simpleProjectilePrefab, Player.LocalClientInstance.GetWandProjectileSpawnPoint().position, Quaternion.identity);
		
		SimpleProjectile simpleProjectile = projectile.GetComponent<SimpleProjectile>();
		
		simpleProjectile.Initialize(simpleWandInventoryItem.ProjectileItemSO);
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
