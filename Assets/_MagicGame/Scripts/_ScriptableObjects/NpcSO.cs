using UnityEngine;

[CreateAssetMenu(fileName = "New Npc", menuName = "Npc")]
public class NpcSO : ScriptableObject
{
    [field: SerializeField] public GameObject NpcPrefab { get; private set; }
    
    [Tooltip("The amount of 'npc space' the NPC take up when spawned")]
    [field: SerializeField] public float SlotAmount { get; private set; }
}
