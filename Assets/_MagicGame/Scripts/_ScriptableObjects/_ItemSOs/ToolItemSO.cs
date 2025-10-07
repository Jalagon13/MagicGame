using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FMODUnity;

namespace ProjectWizard
{
    public enum ToolType
    {
        Pickaxe,
        Axe,
        Sword
    }

    [CreateAssetMenu(fileName = "New Tool", menuName = "Create Item/New Tool")]
    public class ToolItemSO : ItemDataSO
    {
        [field: SerializeField] public ToolType ToolType { get; private set; }
        [field: SerializeField] public int MiningPower { get; private set; } = 1;
        [field: SerializeField] public int Damage { get; private set; } = 4;
        [field: SerializeField] public int Knockback { get; private set; } = 6;
        [field: SerializeField] public float ColliderLength { get; private set; } = 1f;
        [field: SerializeField] public float SwingDuration { get; private set; } = 0.35f;
        [field: SerializeField] public float SwingCooldown { get; private set; } = 0.25f;
        [field: SerializeField] public EventReference HitSound { get; private set; }
        
        private float _detectionBetweenHitsDuration = 0.05f;
        public float DetectionBetweenHitsDuration => _detectionBetweenHitsDuration;


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
}