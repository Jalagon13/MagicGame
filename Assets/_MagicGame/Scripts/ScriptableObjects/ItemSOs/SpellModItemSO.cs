using UnityEngine;

[CreateAssetMenu(fileName = "New Spell Modifier", menuName = "Create Item/New Spell Modifier")]
public class SpellModItemSO : MagicItemSO
{
    [field: SerializeField] public SpellModifier SpellModifierPrefab { get; private set; }
}