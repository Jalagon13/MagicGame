using UnityEngine;

[System.Serializable]
public class BuffConfiguration
{
    [Header("Buff Settings")]
    [Tooltip("The stat this buff will modify")]
    public StatType statType;

    [Tooltip("The type of modification (Flat, Percent, etc.)")]
    public StatModifierType modifierType;

    [Tooltip("The value of the modification")]
    public float modifierValue;

    [Tooltip("Duration of the buff in seconds. Leave at 0 for permanent buffs")]
    public float duration;

    [Tooltip("Display name for this buff (for UI purposes)")]
    public string buffName;

    [Tooltip("Description of what this buff does")]
    [TextArea(2, 4)]
    public string description;

    [Header("Visual Settings")]
    [Tooltip("Icon for this buff (optional)")]
    public Sprite buffIcon;

    [Tooltip("Color tint for the buff icon")]
    public Color iconColor = Color.white;
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
