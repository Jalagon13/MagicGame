using UnityEngine;

public class LifetimeSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private float _additionalLifetime = 1.5f;

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        spellData.Lifetime += _additionalLifetime;
        return spellData;
    }
}
