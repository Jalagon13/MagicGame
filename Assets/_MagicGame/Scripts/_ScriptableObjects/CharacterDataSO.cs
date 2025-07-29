using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "New Character Data", menuName = "CharacterData")]
public class CharacterDataSO : ScriptableObject
{
    [Tooltip("Base HP for character")]
    public int BaseHealth;
    [Tooltip("Base Speed for character")]
    public float BaseSpeed;
    [Tooltip("Duration of invincibility frames when character is hit")]
    public float IFrameDuration = 0.2f;
    [Tooltip("Smaller values = slower transition to desired direction")]
    public float TurnSharpness = 5f;
    [Tooltip("Resistance to knockback effects (0 = no resistance)")]
    public float KnockbackResist = 0f;
    [Tooltip("Respawn Timer")]
    public float RespawnTimerDuration = 0f;
    [Tooltip("Base Health Regen Interval")]
    public float BaseHealthRegenTimeInterval = 0.25f;
    [Tooltip("Base Health Regen Amount")]
    public int BaseHealthRegenAmount = 1;
    [Tooltip("If true, the NPC can be knocked back")]
    public bool CanBeKnockedBack = true;
    [Tooltip("If true, character can die")]
    public bool CanDie = true;

    [Header("Sounds")]
    public EventReference HurtSound;
    public EventReference DeathSound;
    public EventReference WalkSound;

    [Header("Npc Parameters")]
    [Tooltip("Indicates whether the character is an NPC")]
    public bool IsNpc;
    [Tooltip("The amount of 'npc space' the NPC take up when spawned")]
    public float SlotAmount;
    [Tooltip("The String ID of the NPC used for serialization not runtime lookups and networksyncing")]
    public string StringID;
    [Tooltip("Prefab for the NPC")]
    public GameObject NpcPrefab;
    [Tooltip("Loot table for loot dropped.")]
    public List<Loot> LootTable = new();
    
    [Header("AI Parameters")]
    public bool IsFriendly;
    [Tooltip("If true, the NPC will chase the player when detected")]
    public bool WillChasePlayer = true;
    [Tooltip("If true, the NPC only chases the player after being provoked")]
    public bool OnlyChaseWhenProvoked = true;
    [Tooltip("If false, the NPC will remain idle and not move")]
    public bool CanMove = true;
    [Tooltip("Base attack stat for the character")]
    public int BaseAttack;
    [Tooltip("Base defense stat for the character")]
    public int BaseDefense;
    [Tooltip("Speed the character uses when chasing a target")]
    public float PursueSpeed = 4f;
    [Tooltip("Minimum time the NPC will stay idle before changing state")]
    public float MinIdleDuration = 2.5f;
    [Tooltip("Maximum time the NPC will stay idle before changing state")]
    public float MaxIdleDuration = 5f;
    [Tooltip("Radius within which the NPC can detect the player or breadcrumbs")]
    public float DetectionRadius = 15f;
    [Tooltip("Time interval between each detection check")]
    public float DetectionIntervalDuration = 0.5f;
    [Tooltip("Radius within which the NPC can wander randomly")]
    public float WanderRadius = 10f;
    [Tooltip("Distance from the destination at which the NPC stops moving")]
    public float StoppingDistance = 0.25f;
    [Tooltip("Duration the NPC will strafe during movement behavior")]
    public float StrafingDuration = 0.25f;
    [Tooltip("Intensity of the strafing movement")]
    public float StrafeIntensity = 0.5f;
}