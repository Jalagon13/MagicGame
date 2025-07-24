using System;
using System.Collections.Generic;
using System.Text;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Create Item/New Spell")]
public class SpellItemSO : MagicItemSO
{
	[field: Header("Visuals")]
	[field: Tooltip("Actual Prefab for the projectile.")]
	[field: SerializeField] public ServerSpell SpellPrefab { get; private set; }
	
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
	
	[field: Tooltip("The mana cost required to cast this spell.")]
	[field: SerializeField] public int ManaCost { get; private set; } = 5;
	
	[field: Tooltip("The amount of damage this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Damage { get; private set; } = 3;
	
	[field: Tooltip("The amount of knockback this projectile deals upon hitting an enemy.")]
	[field: SerializeField] public int Knockback { get; private set; } = 3;

	[field: Tooltip("How many times this projectile can bounce.")]
	[field: SerializeField] public int BounceCount { get; private set; } = 0;

	[field: Tooltip("How many times this projectile can pierce through enemies.")]
	[field: SerializeField] public int PierceCount { get; private set; } = 0;

	[field: Tooltip("The lifetime in seconds of the projectile.")]
	[field: SerializeField] public float Lifetime { get; private set; } = 2f;

	[field: Tooltip("The speed at which the projectile travels.")]
	[field: SerializeField] public int Speed { get; private set; } = 100;
	
	[field: Tooltip("How much knockback applied to player when this spell recoils")]
	[field: SerializeField] public float Recoil { get; private set; } = 2f;

	[field: Tooltip("Multiplier on fast the player moves when casting this spell")]
	[field: SerializeField] public float HasteMultiplier { get; private set; } = 0.5f;
	
	[field: Tooltip("How accurate the spell is being shot from")]
	[field: SerializeField] public float Scatter { get; private set; } = 0f;
	
	[field: Tooltip("If true, only continue spell sequence after this spell ends or like it despawns")] 
	[field: SerializeField] public bool OnlyContinueAfterSpellEnds { get; private set; } = false;
	
	
	public SyncSpellData GetSyncSpellData(ulong casterNetObjId, BiomeType spawnBiome, List<SpellModItemSO> spellMods = null)
	{
		return new SyncSpellData(GameManager.Instance.GetItemIdFromItemSO(this),
			ManaCost, Damage, Knockback, BounceCount, PierceCount, Speed, Lifetime,
			HasteMultiplier, Scatter, casterNetObjId, OnlyContinueAfterSpellEnds, spawnBiome, spellMods);
	}
	
	public virtual void StartSpell(int slotIndex) // Default behavior, spawn spell on server, assign it to player
	{
		// var syncSpellData = GetSpellDataForLocalClientInstance();
		
		// InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
		// SpellManager.Instance.SpawnSpellServerRpc(syncSpellData, Player.LocalClientInstance.PlayerHand.SpellSpawnTransform.position);
		// SpellManager.Instance.LoadSpell(this, new LoadedSpell(this, syncSpellData, selectedInventoryItem));
	}
}
