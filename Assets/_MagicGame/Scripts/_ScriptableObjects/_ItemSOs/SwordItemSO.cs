using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FMODUnity;

[CreateAssetMenu(fileName = "New Sword", menuName = "Create Item/New Sword")]
public class SwordItemSO : ItemSO
{
	// For custom spell modifiers
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
	
	public override string GetDescription()
	{
		return Description;
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}
}