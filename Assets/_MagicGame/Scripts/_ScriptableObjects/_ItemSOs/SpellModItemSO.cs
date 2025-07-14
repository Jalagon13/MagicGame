using UnityEngine;

[CreateAssetMenu(fileName = "New Spell Modifier", menuName = "Create Item/New Spell Modifier")]
public class SpellModItemSO : MagicItemSO
{
    [field: Tooltip("Actual Prefab for the modifier.")]
    [field: SerializeField] public ServerSpell SpellModPrefab { get; private set; } // Change this to a SpellMod class

    [field: Tooltip("The mana cost required to cast this mod.")]
    [field: SerializeField] public int ManaCost { get; private set; } = 5;
}
