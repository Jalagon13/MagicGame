using UnityEngine;

[CreateAssetMenu(fileName = "New Spell Modifier", menuName = "Create Item/New Spell Modifier")]
public class SpellModItemSO : MagicItemSO
{
    [field: SerializeField] public GameObject SpellModifierPrefab { get; private set; }
    [field: SerializeField] public int ManaCost { get; private set; }
}