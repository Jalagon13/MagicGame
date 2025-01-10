using System;
using UnityEngine;

public class SimpleWandInventoryItem : InventoryItem
{
	public static event EventHandler OnProjectileShot;

	public ItemSO ProjectileItemSO { get; private set; }

	public SimpleWandInventoryItem(ItemSO itemSO, int quantity) : base(itemSO, quantity)
	{
		Debug.Log($"Created simple wand inventory item {itemSO.Name}");
		// Add custom behavior or properties here for wands
	}
	
	public void EquipProjectile(ItemSO projectileItemSO)
	{
		ProjectileItemSO = projectileItemSO;
	}
	
	public bool HasProjectile()
	{
		return ProjectileItemSO != null;
	}
	
	public void UnequipProjectile()
	{
		ProjectileItemSO = null;
	}
}
