using UnityEngine;

public class DamageSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _damage;

    public void ApplyModifier(Spell spell)
    {
        var temp = spell.SpellDataNV.Value;
        temp.Damage += _damage;
        spell.SpellDataNV.Value = temp;
        Debug.Log($"New Damage: {spell.SpellDataNV.Value.Damage}");
    }
}
