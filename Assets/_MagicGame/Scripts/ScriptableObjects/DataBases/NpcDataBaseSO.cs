using System.Collections.Generic;
using UnityEngine;

public class NpcDataBaseSO : ScriptableObject
{
    [field: SerializeField] public List<NpcSO> NpcDataBase { get; private set; } = new List<NpcSO>();
}
