using UnityEngine;

public class HasteSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private float _hasteAmount;

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        spellData.HasteMultiplier = _hasteAmount;
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