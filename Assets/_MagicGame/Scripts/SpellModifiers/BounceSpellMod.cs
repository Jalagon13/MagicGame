using UnityEngine;
using UnityEngine.Tilemaps;

public class BounceSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _bounceAmount = 2;
    
    public void ApplyModifier(Spell spell)
    {
        Debug.Log($"Old BOunce: {spell.SpellDataNV.Value.Bounces}");
        var temp = spell.SpellDataNV.Value;
        temp.Bounces += _bounceAmount;
        spell.SpellDataNV.Value = temp;
        Debug.Log($"New Bounce: {spell.SpellDataNV.Value.Bounces}");
    }
}
