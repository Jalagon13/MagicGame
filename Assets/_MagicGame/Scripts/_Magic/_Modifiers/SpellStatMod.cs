using UnityEngine;

public enum SpellStat
{
    ManaCost,
    Damage,
    Knockback,
    BounceCount,
    PierceCount,
    Speed,
    Lifetime,
    HasteMultiplier
}

public class SpellStatMod : SpellModifier
{
    [SerializeField] 
    private SpellStat _spellStatToEdit;
    
    [Tooltip("Ambiguous amount that can refer to any of the spell stats.")]
    [SerializeField] 
    private int _amount = 0;

    public override SyncSpellData ModifiySpellData(SyncSpellData original, ServerSpell serverSpell = null)
    {
        SyncSpellData modifiedSyncSpellData = original;
        
        switch(_spellStatToEdit)
        {
            case SpellStat.ManaCost:
                modifiedSyncSpellData.ManaCost += _amount;
                break;
            case SpellStat.Damage:
                modifiedSyncSpellData.Damage += _amount;
                break;
            case SpellStat.Knockback:
                modifiedSyncSpellData.Knockback += _amount;
                break;
            case SpellStat.BounceCount:
                modifiedSyncSpellData.BounceCount += _amount;
                break;
            case SpellStat.PierceCount:
                modifiedSyncSpellData.PierceCount += _amount;
                break;
            case SpellStat.Speed:
                modifiedSyncSpellData.Speed += _amount;
                break;
            case SpellStat.Lifetime:
                modifiedSyncSpellData.Lifetime += _amount;
                break;
            case SpellStat.HasteMultiplier:
                modifiedSyncSpellData.HasteMultiplier += _amount;
                break;
        }
        return modifiedSyncSpellData;
    }
}
