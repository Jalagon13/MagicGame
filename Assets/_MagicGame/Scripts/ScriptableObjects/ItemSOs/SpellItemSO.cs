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
	public float Speed;
	public float Lifetime;
	public ulong SpellId;
	public ulong SpawnPlayerId;
	public Vector2 Direction;
	public Vector2 SpawnPoint;
	public BiomeType SpawnBiome;
	public List<int> ModifierArray;

	public SyncSpellData(int spellIndex, int damage, int knockback, float speed, float lifetime, ulong spellId, ulong spawnPlayerId, Vector2 direction, Vector2 spawnPoint, BiomeType spawnBiome, List<int> modifierArray)
	{
		SpellIndex = spellIndex;
		Damage = damage;
		Knockback = knockback;
		Speed = speed;
		Lifetime = lifetime;
		SpellId = spellId;
		SpawnPlayerId = spawnPlayerId;
		Direction = direction;
		SpawnPoint = spawnPoint;
		SpawnBiome = spawnBiome;
		ModifierArray = modifierArray ?? new List<int>(); // Ensure no null lists
	}

	public bool Equals(SyncSpellData other)
	{
		// Check if all primitive properties match
		if (SpellIndex != other.SpellIndex ||
			Damage != other.Damage ||
			Knockback != other.Knockback ||
			Speed != other.Speed ||
			Lifetime != other.Lifetime ||
			SpellId != other.SpellId ||
			SpawnPlayerId != other.SpawnPlayerId ||
			Direction != other.Direction ||
			SpawnPoint != other.SpawnPoint ||
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
		serializer.SerializeValue(ref Speed);
		serializer.SerializeValue(ref Lifetime);
		serializer.SerializeValue(ref SpellId);
		serializer.SerializeValue(ref SpawnPlayerId);
		serializer.SerializeValue(ref Direction);
		serializer.SerializeValue(ref SpawnPoint);
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

	
	[field: SerializeField] public EventReference SpellCast { get; private set; }

	public void CastSpell(WandItemSO wandSO, List<int> modifierArray)
	{
		ulong projectileId = IdGenerator.GenerateRandomId();
		ulong sourcePlayerId = Player.LocalClientInstance.OwnerClientId;
		Debug.Log($"Casting spell {this}");
		int spellIndex = GameManager.Instance.GetItemIdFromItemSO(this);
		Vector2 spawnPoint = NetworkManager.Singleton.ConnectedClients[Player.LocalClientInstance.OwnerClientId].PlayerObject.GetComponent<Player>().MainHand.ProjectileSpawnTransform.position;
		Vector2 baseDirection = (ActionManager.MouseWorldPosition - spawnPoint).normalized;
		float totalSpread = Mathf.Max(0, Accuracy + wandSO.Accuracy);
		float randomAngle = UnityEngine.Random.Range(-totalSpread, totalSpread);
		Vector2 finalDirection = Quaternion.Euler(0, 0, randomAngle) * baseDirection;

		var spellData = new SyncSpellData(spellIndex, Damage, Knockback, Speed, Lifetime, projectileId, sourcePlayerId, finalDirection, spawnPoint, Player.LocalClientInstance.CurrentPlayerBiome.Value, modifierArray);

		GameManager.Instance.SpawnSpellProjectile(spellData);
		SoundManager.Instance.PlayOneShot(SpellCast, Player.LocalClientInstance.MainHand.ProjectileSpawnTransform.position);
	}
}
