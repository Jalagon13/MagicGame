using UnityEngine;
using UnityEngine.Tilemaps;

public class GhostSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private float _ghostDistance = 2;

    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        spellData.GhostDistance += _ghostDistance;
        return spellData;
    }
}
