using UnityEngine;

public class SpeedSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _speedAmount;

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
       spellData.Speed += _speedAmount;
       return spellData;
    }
}
