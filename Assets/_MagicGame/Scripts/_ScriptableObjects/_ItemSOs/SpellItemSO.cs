using System;
using System.Collections.Generic;
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
	public List<int> ModifierArray;

	public SyncSpellData(int spellIndex, int damage, int knockback, int pierces, int bounces, float speed, float lifetime, float ghostDistance, float hasteMultiplier, ulong spellId, ulong ownerPlayerId, BiomeType spawnBiome, List<int> modifierArray)
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
		ModifierArray = modifierArray ?? new List<int>(); // Ensure no null lists
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

		// Check if both lists are either null or have the same count
		if (ModifierArray == null && other.ModifierArray == null)
			return true;
		if (ModifierArray == null || other.ModifierArray == null || ModifierArray.Count != other.ModifierArray.Count)
			return false;

		// Compare each element in the lists
		for (int i = 0; i < ModifierArray.Count; i++)
		{
			if (ModifierArray[i] != other.ModifierArray[i])
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

		// Serialize the list count first
		int modifierCount = ModifierArray?.Count ?? 0;
		serializer.SerializeValue(ref modifierCount);

		if (serializer.IsReader)
		{
			ModifierArray = new List<int>(modifierCount);
		}

		// Serialize each element in the list
		for (int i = 0; i < modifierCount; i++)
		{
			int value = serializer.IsReader ? 0 : ModifierArray[i];
			serializer.SerializeValue(ref value);

			if (serializer.IsReader)
			{
				ModifierArray.Add(value);
			}
		}
	}
}

[CreateAssetMenu(fileName = "New Spell", menuName = "Create Item/New Spell")]
public class SpellItemSO : MagicItemSO
{
	[field: Tooltip("Actual Prefab for the projectile.")]
	[field: SerializeField] public Spell SpellProjectilePrefab { get; private set; }
	
	[field: Tooltip("Spell charging animation.")]
	[field: SerializeField] public GameObject ChargeVFX { get; private set; }
	
	[field: Tooltip("Time it takes to cast this projectile (in seconds).")]
	[field: SerializeField] public float CastTime { get; private set; } = 0.2f;

	[field: Tooltip("The additional delay (in seconds) added to the casting time of this projectile. Negative values reduce the delay.")]
	[field: SerializeField] public float CastDelay { get; private set; } = 0.1f;
	
	[field: Tooltip("The amount of damage this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Damage { get; private set; } = 3;
	
	[field: Tooltip("The mana cost required to cast this projectile.")]
	[field: SerializeField] public int ManaCost { get; private set; } = 5;
	
	[field: Tooltip("The amount of knockback this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Knockback { get; private set; } = 3;

	[field: Tooltip("The amount of randomness in the projectile's trajectory (in degrees). A higher value means more spread.")]
	[field: SerializeField] public float Accuracy { get; private set; } = 1f;
	
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

	[field: SerializeField] public EventReference SpellCast { get; private set; }

	public void NpcShootSpell(Vector2 spawnPos, Vector2 direction, NpcNetworkComponent npc)
	{
		SyncSpellData syncSpellData = new SyncSpellData(
			GameManager.Instance.GetItemIdFromItemSO(this),
			Damage, Knockback, Pierces, Bounces, Speed, Lifetime, GhostDistance, HasteMultiplier,
			IdGenerator.GenerateRandomId(),
			npc.OwnerClientId,
			npc.NpcBiomeType, null);
			
		GameManager.Instance.LoadAndShootSpellServerRpc(syncSpellData, spawnPos, direction);
	}

	public SyncSpellData LoadSpell(WandItemSO wandSO, List<int> modifierArray)
	{
		SyncSpellData syncSpellData = new SyncSpellData(
			GameManager.Instance.GetItemIdFromItemSO(this),
			Damage, Knockback, Pierces, Bounces, Speed, Lifetime, GhostDistance, HasteMultiplier, 
			IdGenerator.GenerateRandomId(),
			Player.LocalClientInstance.OwnerClientId,
			Player.LocalClientInstance.CurrentPlayerBiome.Value, modifierArray);
		
		GameManager.Instance.LoadSpellServerRpc(syncSpellData, Player.LocalClientInstance.MainHand.SpellSpawnTransform.position);
			
		return syncSpellData;
	}
	
	public void ExecuteSpell(WandItemSO wandSO, ulong spellId)
	{
		Vector2 spawnPoint = NetworkManager.Singleton.ConnectedClients[Player.LocalClientInstance.OwnerClientId].PlayerObject.GetComponent<Player>().MainHand.SpellSpawnTransform.position;
		Vector2 baseDirection = (ActionManager.MouseWorldPosition - spawnPoint).normalized;
		float totalSpread = Mathf.Max(0, Accuracy + wandSO.Accuracy);
		float randomAngle = UnityEngine.Random.Range(-totalSpread, totalSpread);
		Vector2 finalDirection = Quaternion.Euler(0, 0, randomAngle) * baseDirection;
		
		Player.LocalClientInstance.PlayerKnockback.ApplyKnockback(ActionManager.MouseWorldPosition, 0, Recoil);
		GameManager.Instance.ExecuteSpellServerRpc(spellId, finalDirection, spawnPoint);
		SoundManager.Instance.PlayOneShot(SpellCast, Player.LocalClientInstance.MainHand.SpellSpawnTransform.position);
	}
	
	public void CancelSpell(ulong spellId)
	{
		Player.LocalClientInstance.PlayerVisuals.StopChargeVfxClientRpc();

		GameManager.Instance.CancelSpellServerRpc(spellId);
	}
}
