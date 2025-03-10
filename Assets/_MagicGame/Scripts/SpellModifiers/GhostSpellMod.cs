using UnityEngine;
using UnityEngine.Tilemaps;

public class GhostSpellMod : MonoBehaviour, ISpellModifier
{
    [SerializeField] private float _ghostDistance = 2;

    public void ApplyModifier(Spell spell)
    {
        var temp = spell.SpellDataNV.Value;
        temp.GhostDistance += _ghostDistance;
        spell.SpellDataNV.Value = temp;
    }
}
