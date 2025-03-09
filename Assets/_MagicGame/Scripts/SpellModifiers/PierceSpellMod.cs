using UnityEngine;

public class PierceSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _pierceAmount = 2;

    public void ApplyModifier(Spell spell)
    {
        var temp = spell.SpellDataNV.Value;
        temp.MaxVictims += _pierceAmount;
        spell.SpellDataNV.Value = temp;
    }
}
