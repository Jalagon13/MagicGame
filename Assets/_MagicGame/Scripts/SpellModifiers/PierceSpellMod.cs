using UnityEngine;

public class PierceSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _pierceAmount = 2;

    public void ApplyModifier(Spell spell)
    {
        var temp = spell.SpellData.Value;
        temp.Pierces += _pierceAmount;
        spell.SpellData.Value = temp;
    }

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        spellData.Pierces += _pierceAmount;
        return spellData;
    }

    public void SelfCastStart(Spell spell = null)
    {

    }

    public void SelfCastUpdate(Spell spell = null)
    {

    }
    
    public void SelfCastEnd(Spell spell = null)
    {

    }
}
