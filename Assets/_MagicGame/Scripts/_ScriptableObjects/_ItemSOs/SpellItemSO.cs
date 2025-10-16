using System;
using System.Collections.Generic;
using System.Text;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

namespace ProjectTinker
{
    [CreateAssetMenu(fileName = "New Spell", menuName = "Create Item/New Spell")]
    public class SpellItemSO : ItemDataSO
    {
        [field: Header("Visuals")]
        [field: Tooltip("Actual Prefab for the projectile.")]
        [field: SerializeField] public ServerSpell SpellPrefab { get; private set; }

        [field: Tooltip("Spell charging animation.")]
        [field: SerializeField] public GameObject ChargeVFX { get; private set; }

        [field: SerializeField] public EventReference SpellOnCastSound { get; private set; }
        [field: SerializeField] public EventReference SpellOnDamageSound { get; private set; }

        [field: Header("Stats")]
        [field: Tooltip("Time it takes for the spell to charge before it is cast.")]
        [field: SerializeField] public float CastTime { get; private set; } = 0.5f;

        [field: Tooltip("Time it takes before the spell can be cast again after casting it.")]
        [field: SerializeField] public float Cooldown { get; private set; } = 0.5f;

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

        [field: Tooltip("Multiplier on fast the player moves when casting this spell")]
        [field: SerializeField] public float HasteMultiplier { get; private set; } = 0.5f;

        [field: Tooltip("How accurate the spell is being shot from")]
        [field: SerializeField] public float Scatter { get; private set; } = 0f;

        [field: Tooltip("If true, holding the fire button will keep this spell active until released. If false, it behaves as a normal single-cast spell.")]
        [field: SerializeField] public bool HoldToCast { get; private set; } = false;

        [field: Tooltip("Materials required to cast this spell. If empty, the spell can be cast without any materials.")]
        [field: SerializeField] public List<InventoryItem> CastingMaterials { get; private set; } = new List<InventoryItem>();

        public SyncSpellData GetSyncSpellData(ulong casterNetObjId, BiomeType spawnBiome)
        {
            return new SyncSpellData(GameDataRegistry.Instance.GetItemIdFromItemData(this),
                Damage, Knockback, BounceCount, PierceCount, Speed, Lifetime,
                HasteMultiplier, Scatter, casterNetObjId, HoldToCast, spawnBiome);
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

        // public virtual void StartSpell(int slotIndex) // Default behavior, spawn spell on server, assign it to player
        // {
        // 	// var syncSpellData = GetSpellDataForLocalClientInstance();

        // 	// InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
        // 	// SpellManager.Instance.SpawnSpellServerRpc(syncSpellData, Player.LocalClientInstance.PlayerHand.SpellSpawnTransform.position);
        // 	// SpellManager.Instance.LoadSpell(this, new LoadedSpell(this, syncSpellData, selectedInventoryItem));
        // }
    }
}
