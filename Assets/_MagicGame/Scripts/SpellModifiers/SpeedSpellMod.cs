using UnityEngine;

public class SpeedSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _speedAmount;
    [SerializeField] private float _playerSpeedDecayMult = 1.5f;

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
       spellData.Speed += _speedAmount;
       return spellData;
    }
}
