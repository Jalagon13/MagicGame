using UnityEngine;

namespace ProjectWizard
{
    [System.Serializable]
    public class BuffConfiguration
    {
        [Header("Buff Settings")]
        [Tooltip("The stat this buff will modify")]
        public StatType statType;

        [Tooltip("The type of modification (Flat, Percent, etc.)")]
        public StatModifierType modifierType;

        [Tooltip("Flat value to add/subtract (used when modifier type is Flat)")]
        public float flatValue;

        [Tooltip("Percentage value to add/subtract (used when modifier type is Percent)")]
        [Range(0f, 100f)]
        public float percentValue;

        [Tooltip("Duration of the buff in seconds. Leave at 0 for permanent buffs")]
        public float duration;

        [Tooltip("Display name for this buff (for UI purposes)")]
        public string buffName;

        [Header("Visual Settings")]
        [Tooltip("Icon for this buff (optional)")]
        public Sprite buffIcon;

        [Tooltip("Color tint for the buff icon")]
        public Color iconColor = Color.white;

        [Tooltip("Description of what this buff does")]
        [TextArea(2, 4)]
        public string description;
    }

    public enum StatType
    {
        MaxHealth,
        Defense,
        MovementSpeed,
        // Add more stat types as needed
        // AttackDamage,
        // MagicDamage,
        // CriticalChance,
        // etc.
    }
}
