using UnityEngine;

public class DamageSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _damage;

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        spellData.Damage += _damage;
        return spellData;
    }
}
