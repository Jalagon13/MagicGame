using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FMODUnity;

[CreateAssetMenu(fileName = "New Tool", menuName = "Create Item/New Tool")]
public class ToolItemSO : ItemSO
{
	[field: SerializeField] public int MiningPower { get; private set; }
	[field: SerializeField] public float MiningRange { get; private set; }
	[field: SerializeField] public int MeleeDamage { get; private set; }
	[field: SerializeField] public int Knockback { get; private set; }
	[field: SerializeField] public float SwingCooldown { get; private set; } = 0.25f;
	[field: SerializeField] public EventReference HitSound { get; private set; }

	public void PlayHitSound()
	{
	    SoundManager.Instance.PlayOneShot(HitSound, Player.LocalClientInstance.transform.position);
	}

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}

	public bool PlayerWithinMiningRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, ActionManager.MouseWorldPosition) <= MiningRange;
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