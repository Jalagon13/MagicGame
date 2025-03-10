using UnityEngine;

public interface ISpellModifier
{
    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null);
}