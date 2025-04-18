using System;
using System.Collections.Generic;
using System.Text;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public struct SyncSpellData : IEquatable<SyncSpellData>, INetworkSerializable
{
	public int SpellIndex;
	public int Damage;
	public int Knockback;
	public float Speed;
	public float Lifetime;
	public float HasteMultiplier;
	public ulong SpellId;
	public ulong OwnerPlayerId;
	public ulong InventorySlotId;
	public bool DespawnIfFocusSlotChanged;
	public BiomeType SpawnBiome;

	public SyncSpellData(int spellIndex, int damage, int knockback, float speed, float lifetime, float hasteMultiplier, ulong spellId, ulong ownerPlayerId, ulong inventorySlotId, bool despawnIfFocusSlotChanged, BiomeType spawnBiome)
	{
		SpellIndex = spellIndex;
		Damage = damage;
		Knockback = knockback;
		Speed = speed;
		Lifetime = lifetime;
		HasteMultiplier = hasteMultiplier;
		SpellId = spellId;
		OwnerPlayerId = ownerPlayerId;
		InventorySlotId = inventorySlotId;
		DespawnIfFocusSlotChanged = despawnIfFocusSlotChanged;
		SpawnBiome = spawnBiome;
	}

	public bool Equals(SyncSpellData other)
	{
		// Check if all primitive properties match
		if (SpellIndex != other.SpellIndex ||
			Damage != other.Damage ||
			Knockback != other.Knockback ||
			Speed != other.Speed ||
			Lifetime != other.Lifetime ||
			HasteMultiplier != other.HasteMultiplier ||
			SpellId != other.SpellId ||
			OwnerPlayerId != other.OwnerPlayerId ||
			InventorySlotId != other.InventorySlotId ||
			DespawnIfFocusSlotChanged != other.DespawnIfFocusSlotChanged ||
			SpawnBiome != other.SpawnBiome)
		{
			return false;
		}

		return true;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref SpellIndex);
		serializer.SerializeValue(ref Damage);
		serializer.SerializeValue(ref Knockback);
		serializer.SerializeValue(ref Speed);
		serializer.SerializeValue(ref Lifetime);
		serializer.SerializeValue(ref HasteMultiplier);
		serializer.SerializeValue(ref SpellId);
		serializer.SerializeValue(ref OwnerPlayerId);
		serializer.SerializeValue(ref InventorySlotId);
		serializer.SerializeValue(ref DespawnIfFocusSlotChanged);
		serializer.SerializeValue(ref SpawnBiome);
	}
}

[CreateAssetMenu(fileName = "New Spell", menuName = "Create Item/New Spell")]
public class SpellItemSO : ItemSO
{
	[field: Header("Visuals")]
	[field: Tooltip("Actual Prefab for the projectile.")]
	[field: SerializeField] public Spell SpellProjectilePrefab { get; private set; }
	
	[field: Tooltip("Spell charging animation.")]
	[field: SerializeField] public GameObject ChargeVFX { get; private set; }

	[field: Tooltip("Image displayed when equipped to a spellbook")]
	[field: SerializeField] public Sprite SpellPortrait { get; private set; }
	[field: SerializeField] public EventReference SpellCast { get; private set; }

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
	
	public SyncSpellData GetSpellDataForLocalClientInstance()
	{
		InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);

		return new SyncSpellData(
			GameManager.Instance.GetItemIdFromItemSO(this),
			Damage, Knockback, Speed, Lifetime, HasteMultiplier, 
			IdGenerator.GenerateRandomId(),
			Player.LocalClientInstance.OwnerClientId,
			selectedInventoryItem.Id,
			DespawnIfFocusSlotChanged,
			Player.LocalClientInstance.CurrentPlayerBiome.Value);
	}

	public SyncSpellData LoadSpell(SpellBookItemSO wandSO)
	{
		var syncSpellData = GetSpellDataForLocalClientInstance();
		Debug.Log($"LoadSpell in Spellitemso {syncSpellData.SpellId}");
		GameManager.Instance.SpawnSpellServerRpc(syncSpellData, Player.LocalClientInstance.MainHand.SpellSpawnTransform.position);
		return syncSpellData;
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
