using UnityEngine;
using UnityEngine.Tilemaps;

public class BounceSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private int _bounceAmount = 2;
    
    
    public SyncSpellData ModifiySpellData(SyncSpellData spellData, Spell spell = null)
    {
        spellData.Bounces += _bounceAmount;
        return spellData;
    }
}
