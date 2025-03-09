using UnityEngine;

public class SpeedSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _speedAmount;

    public void ApplyModifier(Spell spell)
    {
        var temp = spell.SpellDataNV.Value;
        temp.Speed += _speedAmount;
        spell.SpellDataNV.Value = temp;
    }
}
