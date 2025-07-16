using UnityEngine;

public class DamageSpellMod : SpellModifier
{
    [SerializeField] private int _damageAmount = 10;

    public override SyncSpellData ModifiySpellData(SyncSpellData original)
    {
        SyncSpellData modified = original;
        modified.Speed += _damageAmount;
        return modified;
    }
}
