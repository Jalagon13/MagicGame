using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu]
public class NpcSO : ScriptableObject
{
    [PropertyTooltip("Amount of 'space' an Npc can take up")]
    public float SlotAmount;
    public GameObject Prefab;
}
