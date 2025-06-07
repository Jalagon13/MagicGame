using System;
using System.Collections.Generic;
using System.Text;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Create Item/New Spell")]
public class SpellItemSO : ItemSO
{
	[field: Header("Visuals")]
	[field: Tooltip("Actual Prefab for the projectile.")]
	[field: SerializeField] public Spell SpellProjectilePrefab { get; private set; }
	
	[field: Tooltip("Spell Icon that will be displayed as the actual spell when equipped")]
	[field: SerializeField] public Sprite SpellUIDisplaySprite { get; private set; }

	[field: Tooltip("Spell charging animation.")]
	[field: SerializeField] public GameObject ChargeVFX { get; private set; }

	[field: SerializeField] public EventReference SpellCastSound { get; private set; }

	[field: Header("Stats")]
	[field: Tooltip("Time it takes to cast this projectile (in seconds).")]
	[field: SerializeField] public float CastTime { get; private set; } = 0.2f;

	[field: Tooltip("The cooldown time (in seconds) before this spell can be cast again. A lower value means the spell can be reused more quickly.")]
	[field: SerializeField] public float Cooldown { get; private set; } = 0.1f;
	
	[field: Tooltip("The mana cost required to cast this projectile.")]
	[field: SerializeField] public int ManaCost { get; private set; } = 5;
	
	[field: Tooltip("The amount of damage this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Damage { get; private set; } = 3;
	
	[field: Tooltip("The amount of knockback this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Knockback { get; private set; } = 3;

	[field: Tooltip("The lifetime in seconds of the projectile.")]
	[field: SerializeField] public float Lifetime { get; private set; } = 2f;

	[field: Tooltip("The speed at which the projectile travels.")]
	[field: SerializeField] public int Speed { get; private set; } = 100;
	
	[field: Tooltip("How much knockback applied to player when this spell recoils")]
	[field: SerializeField] public float Recoil { get; private set; } = 2f;

	[field: Tooltip("Multiplier on fast the player moves when casting this spell")]
	[field: SerializeField] public float HasteMultiplier { get; private set; } = 0.5f;
	
	[field: Tooltip("Should the spell be despawned if the slot it was cast from changes during spell lifetime")]
	[field: SerializeField] public bool DespawnIfFocusSlotChanged { get; private set; }
	
	[field: Tooltip("Should the spell be continuous cast while holding down cast button")]
	[field: SerializeField] public bool IsContinuousCast { get; private set; } = false;
	
	public SyncSpellData GetSpellDataForLocalClientInstance(int wandSlotIndex)
	{
		InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);

		return new SyncSpellData(
			GameManager.Instance.GetItemIdFromItemSO(this),
			ManaCost, Damage, Knockback, wandSlotIndex, Speed, Lifetime, HasteMultiplier, 
			IdGenerator.GenerateRandomId(),
			Player.LocalClientInstance.OwnerClientId,
			selectedInventoryItem.Id,
			DespawnIfFocusSlotChanged,
			IsContinuousCast,
			Player.LocalClientInstance.CurrentBiome.Value);
	}
	
	public virtual void StartSpell(int slotIndex) // Default behavior, spawn spell on server, assign it to player
	{
		var syncSpellData = GetSpellDataForLocalClientInstance(slotIndex);
		
		InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
		SpellManager.Instance.SpawnSpellServerRpc(syncSpellData, Player.LocalClientInstance.PlayerHand.SpellSpawnTransform.position);
		SpellManager.Instance.LoadSpell(this, new LoadedSpell(this, syncSpellData, selectedInventoryItem));
	}
	
	public override InventoryItem CreateInventoryItem(int quantity)
	{
		return new InventoryItem(this, quantity);
	}

	public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
	{
		return _baseActionCooldown;
	}

	public override string GetDescription()
	{
		StringBuilder description = new();
		description.Append($"Can be placed in a wand slot<br>");
		description.Append($"{GetDescriptionBreak()}");

		return description.ToString();
	}
}
