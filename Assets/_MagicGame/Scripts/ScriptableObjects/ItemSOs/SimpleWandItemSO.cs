using UnityEngine;

[CreateAssetMenu(fileName = "New Simple Wand Item", menuName = "Create Item/New Simple Wand Item")]
public class SimpleWandItemSO : ItemSO
{
	[SerializeField] private GameObject _simpleProjectilePrefab;
	[SerializeField] private ItemSO _test;

	public ItemSO GetTestItem()
	{
		return _test;
	}

	public override void ExecutePrimaryAction(InventoryItem inventoryItem)
	{
		var simpleWandInventoryItem = inventoryItem as SimpleWandInventoryItem;
		
		if(_test != null)
		{
			GameObject projectile = Instantiate(_simpleProjectilePrefab, Player.LocalClientInstance.GetWandProjectileSpawnPoint().position, Quaternion.identity);
			
			SimpleProjectile simpleProjectile = projectile.GetComponent<SimpleProjectile>();
			
			Vector2 direction = ((Vector3)ActionManager.MouseWorldPosition - projectile.transform.position).normalized;
			
			simpleProjectile.Initialize(_test, direction);
		}
		else
		{
			Debug.LogWarning("No Test Item equiped in SO");
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
