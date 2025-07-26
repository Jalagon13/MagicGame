using System.Collections.Generic;
using UnityEngine;

public class PayloadCastItemSO : MagicItemSO
{
    [field: Tooltip("This is the amount of spells that can be cast in this payload")]
    [field: SerializeField]
    public int SpellCastAmount { get; private set; }

    public List<SpellCastGroup> BuildSpellGroups(MagicItemSO[] magicArray, ref int i, List<SpellModItemSO> currentMods)
    {
        List<SpellMetaData> groupedSpells = new();
        int count = 0;
        i++;

        while (i < magicArray.Length && count < SpellCastAmount)
        {
            if (magicArray[i] is SpellModItemSO mod)
            {
                currentMods.Add(mod);
            }
            else if (magicArray[i] is SpellItemSO spell)
            {
                groupedSpells.Add(new SpellMetaData(spell, new List<SpellModItemSO>(currentMods)));
                currentMods.Clear();
                count++;
            }
            i++;
        }

        return new List<SpellCastGroup> {
            new SpellCastGroup(groupedSpells, this)
        };
    }
}
