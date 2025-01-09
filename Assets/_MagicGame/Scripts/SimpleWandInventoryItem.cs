using System;
using UnityEngine;

public class SimpleWandInventoryItem : InventoryItem
{
	public static event EventHandler OnProjectileShot;

	public ItemSO ProjectileItemSO { get; private set; }
	public int ProjectileQuantity { get; private set; }

	public SimpleWandInventoryItem(ItemSO itemSO, int quantity) : base(itemSO, quantity)
	{
		Debug.Log($"Created simple wand inventory item {itemSO.Name}");
		// Add custom behavior or properties here for wands
	}
	
	public void EquipProjectile(ItemSO projectileItemSO, int quantity)
	{
		ProjectileItemSO = projectileItemSO;
		ProjectileQuantity = quantity;
	}
	
	public bool HasProjectile()
	{
		return ProjectileItemSO != null;
	}
	
	public void UnequipProjectile()
	{
		ProjectileItemSO = null;
		ProjectileQuantity = 0;
	}
	
	public void RemoveProjectile()
	{
		ProjectileQuantity--;
		if(ProjectileQuantity <= 0)
		{
			UnequipProjectile();
		}
		
		OnProjectileShot?.Invoke(this, EventArgs.Empty);
	}
}
