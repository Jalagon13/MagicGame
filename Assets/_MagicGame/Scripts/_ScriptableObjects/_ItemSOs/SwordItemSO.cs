using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FMODUnity;

[CreateAssetMenu(fileName = "New Sword", menuName = "Create Item/New Sword")]
public class SwordItemSO : ItemDataSO
{
	[field: SerializeField] public int Damage { get; private set; } = 4;
	[field: SerializeField] public int Knockback { get; private set; } = 6;
	[field: SerializeField] public float DetectionBetweenHitsDuration { get; private set; } = 0.05f;
	[field: SerializeField] public float ColliderLength { get; private set; } = 1f;
	[field: SerializeField] public float SwingDuration { get; private set; } = 0.35f;
	[field: SerializeField] public float SwingCooldown { get; private set; } = 0.25f;
	[field: SerializeField] public EventReference HitSound { get; private set; }

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}

	public override string GetDescription()
	{
		return Description;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}