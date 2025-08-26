using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

[CreateAssetMenu(fileName = "New Resource Data", menuName = "Data/ResourceData")]
public class ResourceDataSO : ScriptableObject
{
    public static float InteractDistance = 2.75f;

    [Header("General Settings")]
    [Tooltip("Name of the resource world object")]
    public string StringID;
    [Tooltip("If true, the player can pass through this resource")]
    public bool PassThrough = false;
    [Tooltip("If true, this resource can be destroyed")]
    public bool CanBeDestroyed = true;
    [Tooltip("Prefab for this Resource")]
    public ResourceObject ResourcePrefab;

    [Space]
    [Header("Mining & Harvesting")]
    [Tooltip("Hardness value determining mining speed")]
    public float Hardness = 1f;
    [Tooltip("Tool type required to harvest this resource")]
    public ToolType ToolTypeNeededForHarvest;

    [Space]
    [Header("Loot & Drops")]
    [Tooltip("Loot table for items dropped by this resource")]
    public List<Loot> Table = new();

    [Space]
    [Header("Sounds")]
    [Tooltip("Sound played while mining this resource")]
    public EventReference MiningSound;
    [Tooltip("Sound played when the resource is destroyed")]
    public EventReference ResourceDestroyed;
    [Tooltip("Sound played when the resource is placed")]
    public EventReference PlaceSound;
}
