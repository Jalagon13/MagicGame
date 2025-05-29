using System;
using UnityEngine;

[Serializable]
public class ActionConfig
{
    [Tooltip("Could be damage, could be healing, or other things. This is a base, nominal value that will get modified by game logic when the action takes effect")]
    public int Amount;

    [Tooltip("How much it costs in Mana to play this Action")]
    public int ManaCost;

    [Tooltip("Duration in seconds that this Action takes to play")]
    public float DurationSeconds;
}