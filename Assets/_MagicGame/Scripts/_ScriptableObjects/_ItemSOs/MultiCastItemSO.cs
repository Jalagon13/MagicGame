using System;
using System.Collections.Generic;
using System.Text;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;


[CreateAssetMenu(fileName = "New MultiCast", menuName = "Create Item/New MultiCast")]
public class MultiCastItemSO : MagicItemSO
{
    [field: Tooltip("This is the amount of spells that can be cast at once")]
    [field: SerializeField] 
    public int SpellCastAmount { get; private set; }
}