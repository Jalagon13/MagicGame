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
	public int Pierces;
	public int Bounces;
	public float Speed;
	public float Lifetime;
	public float GhostDistance;
	public float HasteMultiplier;
	public ulong SpellId;
	public ulong OwnerPlayerId;
	public BiomeType SpawnBiome;

	public SyncSpellData(int spellIndex, int damage, int knockback, int pierces, int bounces, float speed, float lifetime, float ghostDistance, float hasteMultiplier, ulong spellId, ulong ownerPlayerId, BiomeType spawnBiome)
	{
		SpellIndex = spellIndex;
		Damage = damage;
		Knockback = knockback;
		Pierces = pierces;
		Bounces = bounces;
		Speed = speed;
		Lifetime = lifetime;
		GhostDistance = ghostDistance;
		HasteMultiplier = hasteMultiplier;
		SpellId = spellId;
		OwnerPlayerId = ownerPlayerId;
		SpawnBiome = spawnBiome;
	}

	public bool Equals(SyncSpellData other)
	{
		// Check if all primitive properties match
		if (SpellIndex != other.SpellIndex ||
			Damage != other.Damage ||
			Knockback != other.Knockback ||
			Pierces != other.Pierces ||
			Bounces != other.Bounces ||
			Speed != other.Speed ||
			Lifetime != other.Lifetime ||
			GhostDistance != other.GhostDistance ||
			HasteMultiplier != other.HasteMultiplier ||
			SpellId != other.SpellId ||
			OwnerPlayerId != other.OwnerPlayerId ||
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
		serializer.SerializeValue(ref Pierces);
		serializer.SerializeValue(ref Bounces);
		serializer.SerializeValue(ref Speed);
		serializer.SerializeValue(ref Lifetime);
		serializer.SerializeValue(ref GhostDistance);
		serializer.SerializeValue(ref HasteMultiplier);
		serializer.SerializeValue(ref SpellId);
		serializer.SerializeValue(ref OwnerPlayerId);
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

	[field: Tooltip("How many victims to damage before it stops")]
	[field: SerializeField] public int Pierces { get; private set; } = 1;

	[field: Tooltip("How many times it bounces before it stops")]
	[field: SerializeField] public int Bounces { get; private set; } = 1;
	
	[field: Tooltip("How far it can passthrough wall tiles")]
	[field: SerializeField] public float GhostDistance { get; private set; } = 0f;

	[field: Tooltip("Multiplier on fast the player moves when casting this spell")]
	[field: SerializeField] public float HasteMultiplier { get; private set; } = 0.5f;

	public void NpcShootSpell(Vector2 spawnPos, Vector2 direction, NpcNetworkComponent npc)
	{
		SyncSpellData syncSpellData = new SyncSpellData(
			GameManager.Instance.GetItemIdFromItemSO(this),
			Damage, Knockback, Pierces, Bounces, Speed, Lifetime, GhostDistance, HasteMultiplier,
			IdGenerator.GenerateRandomId(),
			npc.OwnerClientId,
			npc.NpcBiomeType);
			
		GameManager.Instance.LoadAndShootSpellServerRpc(syncSpellData, spawnPos, direction);
	}

	public SyncSpellData LoadSpell(SpellBookItemSO wandSO)
	{
		Debug.Log($"player id: {Player.LocalClientInstance.OwnerClientId}");
		SyncSpellData syncSpellData = new SyncSpellData(
			GameManager.Instance.GetItemIdFromItemSO(this),
			Damage, Knockback, Pierces, Bounces, Speed, Lifetime, GhostDistance, HasteMultiplier, 
			IdGenerator.GenerateRandomId(),
			Player.LocalClientInstance.OwnerClientId,
			Player.LocalClientInstance.CurrentPlayerBiome.Value);
		
		GameManager.Instance.LoadSpellServerRpc(syncSpellData, Player.LocalClientInstance.MainHand.SpellSpawnTransform.position);
			
		return syncSpellData;
	}
	
	public void ExecuteSpell(SpellBookItemSO wandSO, ulong spellId)
	{
		Vector2 spawnPoint = NetworkManager.Singleton.ConnectedClients[Player.LocalClientInstance.OwnerClientId].PlayerObject.GetComponent<Player>().MainHand.SpellSpawnTransform.position;
		Vector2 baseDirection = (ActionManager.MouseWorldPosition - spawnPoint).normalized;
		
		Player.LocalClientInstance.PlayerKnockback.ApplyKnockback(ActionManager.MouseWorldPosition, 0, Recoil);
		GameManager.Instance.ExecuteSpellServerRpc(spellId, baseDirection, spawnPoint);
		SoundManager.Instance.PlayOneShot(SpellCast, Player.LocalClientInstance.MainHand.SpellSpawnTransform.position);
	}
	
	public void CancelSpell(ulong spellId)
	{
		Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();

		GameManager.Instance.CancelSpellServerRpc(spellId);
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
