using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellDisplayUI : MonoBehaviour
{
    [field: SerializeField] public GameObject SpellDisplaySlotUIPrefab { get; private set; }
    
    private Dictionary<int, SpellDisplaySlotUI> _spellSlotDatabase = new Dictionary<int, SpellDisplaySlotUI>();

    private void Start()
    {
        SpellManager.Instance.OnSpellArrayUpdated += UpdateSpellDisplay;
    }

    private void UpdateSpellDisplay(object sender, EventArgs e)
    {
        ClearSpells();

        if (SpellManager.Instance.SpellItemArray != null)
        {
            for(int i = 0; i < SpellManager.Instance.SpellItemArray.Length; i++)
            {
                if(SpellManager.Instance.SpellItemArray[i] == null) continue;

                SpellItemSO spell = SpellManager.Instance.SpellItemArray[i];

                GameObject spellDisplaySlotUI = Instantiate(SpellDisplaySlotUIPrefab, transform);
                SpellDisplaySlotUI slotUI = spellDisplaySlotUI.GetComponent<SpellDisplaySlotUI>();
                slotUI.SetSpell(spell, i);

                int spellId = GameManager.Instance.GetItemIdFromItemSO(spell);
                _spellSlotDatabase[spellId] = slotUI;
            }
        }
    }

    private void ClearSpells()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        _spellSlotDatabase.Clear();
    }

    private void OnDestroy()
    {
        SpellManager.Instance.OnSpellArrayUpdated -= UpdateSpellDisplay;
    }
}
