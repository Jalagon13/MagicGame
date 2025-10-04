using FMODUnity;
using UnityEngine;
using System.Collections.Generic;

namespace ProjectWizard
{
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Create Item/New Consumable")]
    public class ConsumableItemSO : ItemDataSO
    {
        [field: SerializeField] public EventReference ConsumeSound { get; private set; }

        [Header("Health Restoration")]
        [field: Tooltip("Health gained when consumed")]
        [field: SerializeField] public int HealthGain { get; private set; }

        [field: Tooltip("Duration of restoration cooldown in seconds (prevents health restoration from other consumables)")]
        [field: SerializeField] public float RestorationCooldownDuration { get; private set; } = 60f;

        [Header("Buffs")]
        [field: Tooltip("List of buffs this consumable applies")]
        [field: SerializeField] public List<BuffConfiguration> Buffs { get; private set; } = new List<BuffConfiguration>();

        public override float ExecuteItemAction(InventoryItem inventoryItem, PlayerHand playerHand)
        {
            bool gainedStat = false;
            var characterStats = Player.Instance.ServerCharacter.Stats;

            // Check if we can restore health (not under restoration cooldown)
            bool canRestoreHealth = HealthGain > 0 && !characterStats.IsUnderRestorationCooldown();

            if (canRestoreHealth && !Player.Instance.ServerCharacter.NetHealthState.IsFullHp())
            {
                Player.Instance.ServerCharacter.NetHealthState.AddHp(HealthGain);
                gainedStat = true;

                // Apply restoration cooldown if this item restores health
                if (RestorationCooldownDuration > 0)
                {
                    var cooldownDebuff = RestorationCooldownDebuff.CreateCooldownDebuff(RestorationCooldownDuration);
                    characterStats.AddBuff(cooldownDebuff);
                }
            }

            // Apply all configured buffs
            foreach (var buffConfig in Buffs)
            {
                var targetStat = characterStats.GetStatByType(buffConfig.statType);
                if (targetStat != null)
                {
                    var modifier = new StatModifier(
                        buffConfig.modifierValue,
                        buffConfig.modifierType,
                        this // Use this consumable as the source
                    );

                    var buff = new Buff(targetStat, modifier, buffConfig.duration > 0 ? buffConfig.duration : null);
                    characterStats.AddBuff(buff);
                    gainedStat = true;
                }
            }

            if (gainedStat)
            {
                Debug.Log($"Consumed {name} - Applied {Buffs.Count} buffs" + (canRestoreHealth ? $" and restored {HealthGain} health" : ""));
                SoundManager.Instance.PlayOneShot(ConsumeSound, Player.Instance.transform.position);
                InventoryManager.Instance.RemoveItem(this, 1); // Note to future self: This implementation is bugged and will need fixing later
            }

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
}