using UnityEngine;

public class PierceSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _pierceAmount = 2;

    public void ApplyModifier(Spell spell)
    {
        var temp = spell.SpellDataNV.Value;
        temp.Pierces += _pierceAmount;
        spell.SpellDataNV.Value = temp;
    }

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        spellData.Pierces += _pierceAmount;
        return spellData;
    }
}
