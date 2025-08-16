using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpellDisplayUI : NetworkBehaviour
{
    [SerializeField] 
    private GameObject _spellDisplaySlotUIPrefab;
    
    private Dictionary<int, SpellDisplaySlotUI> _spellSlotDatabase = new Dictionary<int, SpellDisplaySlotUI>();

    private void Awake()
    {
        if(NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback += RegisterUpdateSpellDisplayCallback;
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientConnectedCallback -= RegisterUpdateSpellDisplayCallback;
        }

        if (Player.Instance != null)
        {
            Player.Instance.SpellCastController.OnSpellArrayUpdated -= UpdateSpellDisplay;
        }
    }

    private void RegisterUpdateSpellDisplayCallback(ulong obj)
    {
        if(NetworkManager.LocalClientId != obj) return;

        Player.Instance.SpellCastController.OnSpellArrayUpdated += UpdateSpellDisplay;
    }

    private void UpdateSpellDisplay(object sender, EventArgs e)
    {
        ClearSpells();

        if (Player.Instance.SpellCastController.SpellArray != null)
        {
            for(int i = 0; i < Player.Instance.SpellCastController.SpellArray.Count; i++)
            {
                if(Player.Instance.SpellCastController.SpellArray[i] == null) continue;

                SpellItemSO spell = Player.Instance.SpellCastController.SpellArray[i];

                GameObject spellDisplaySlotUI = Instantiate(_spellDisplaySlotUIPrefab, transform);
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
}
