using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FMODUnity;

public enum ToolType
{
	Pickaxe, Axe, Sword, Shovel, None
}

[CreateAssetMenu(fileName = "New Tool", menuName = "Create Item/New Tool")]
public class ToolItemSO : ItemSO
{
	[field: SerializeField] public ToolType ToolType { get; private set; }
	[field: SerializeField] public int MiningPower { get; private set; }
	[field: SerializeField] public float MiningRange { get; private set; }
	[field: SerializeField] public int Damage { get; private set; }
	[field: SerializeField] public int Knockback { get; private set; }
	[field: SerializeField] public float DetectionBetweenHitsDuration { get; private set; } = 0.05f;
	[field: SerializeField] public float ColliderLength { get; private set; } = 1f;
	[field: SerializeField] public float SwingDuration { get; private set; } = 0.35f;
	[field: SerializeField] public float SwingCooldown { get; private set; } = 0.25f;
	[field: SerializeField] public EventReference HitSound { get; private set; }

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}

	public bool PlayerWithinMiningRangeOfMouse()
	{
		return Vector2.Distance(Player.Instance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
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