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
        if(SpellManager.Instance.SpellItemArray != null)
        {
            foreach (SpellItemSO spell in SpellManager.Instance.SpellItemArray)
            {
                GameObject spellDisplaySlotUI = Instantiate(SpellDisplaySlotUIPrefab, transform);
                SpellDisplaySlotUI slotUI = spellDisplaySlotUI.GetComponent<SpellDisplaySlotUI>();
                slotUI.SetSpell(spell);

                int spellId = GameManager.Instance.GetItemIdFromItemSO(spell);
                _spellSlotDatabase[spellId] = slotUI;
            }
        }
        else
        {
            ClearSpells();
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
