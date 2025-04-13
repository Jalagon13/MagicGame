using System;
using System.Collections.Generic;
using UnityEngine;

public class SpellDisplayUI : MonoBehaviour
{
    [field: SerializeField] public GameObject SpellDisplaySlotUIPrefab { get; private set; }
    
    private Dictionary<int, SpellDisplaySlotUI> _spellSlotDatabase = new Dictionary<int, SpellDisplaySlotUI>();

    private void Start()
    {
        MagicManager.Instance.OnSpellbookUpdated += UpdateSpellDisplay;
        MagicManager.Instance.OnSelectedSpellUpdated += UpdateSelectedSpell;
    }

    private void UpdateSpellDisplay(object sender, EventArgs e)
    {
        if(MagicManager.Instance.HasEquippedSpellBook)
        {
            foreach (SpellItemSO spell in MagicManager.Instance.GetSpells())
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

    private void UpdateSelectedSpell(object sender, EventArgs e)
    {
        SpellItemSO selectedSpell = MagicManager.Instance.SelectedSpell;
        
        if(selectedSpell != null)
        {
            int selectedSpellId = GameManager.Instance.GetItemIdFromItemSO(selectedSpell);
            
            foreach (var kvp in _spellSlotDatabase)
            {
                if (kvp.Key == selectedSpellId)
                {
                    kvp.Value.SelectSlot();
                }
                else
                {
                    kvp.Value.DeselectSlot();
                }
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
        MagicManager.Instance.OnSpellbookUpdated -= UpdateSpellDisplay;
        MagicManager.Instance.OnSelectedSpellUpdated -= UpdateSelectedSpell;
    }
}
