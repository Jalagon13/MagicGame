using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New RapidCast", menuName = "Create Item/New RapidCast")]
public class RapidCastItemSO : PayloadCastItemSO
{
    [field: Tooltip("SpellDelay between each spell cast in seconds.")]
    [field: SerializeField]
    public float SpellDelay { get; private set; } = 0.2f;
}
