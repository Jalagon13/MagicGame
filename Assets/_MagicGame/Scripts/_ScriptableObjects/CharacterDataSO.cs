using UnityEngine;

[CreateAssetMenu(fileName = "New Character Data", menuName = "CharacterData")]
public class CharacterDataSO : ScriptableObject
{
    [Tooltip("Base HP for character")]
    public int BaseHP;
    [Tooltip("Base MP for character")]
    public int BaseMP;
    [Tooltip("Base Speed for character")]
    public float BaseSpeed;
    [Tooltip("Duration of invincibility frames when character is hit")]
    public float IFrameDuration { get; internal set; }
    [Tooltip("Indicates whether the character is an NPC")]
    public bool IsNpc;
}