using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SpellDisplayUI : NetworkBehaviour
{
    [field: SerializeField] public GameObject SpellDisplaySlotUIPrefab { get; private set; }
    
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

        if (Player.LocalClientInstance != null)
        {
            Player.LocalClientInstance.SpellInputHandler.OnSpellArrayUpdated -= UpdateSpellDisplay;
        }
    }

    private void RegisterUpdateSpellDisplayCallback(ulong obj)
    {
        if(NetworkManager.LocalClientId != obj) return;

        Player.LocalClientInstance.SpellInputHandler.OnSpellArrayUpdated += UpdateSpellDisplay;
    }

    private void UpdateSpellDisplay(object sender, EventArgs e)
    {
        ClearSpells();

        if (Player.LocalClientInstance.SpellInputHandler.EquippedSpells != null)
        {
            for(int i = 0; i < Player.LocalClientInstance.SpellInputHandler.EquippedSpells.Length; i++)
            {
                if(Player.LocalClientInstance.SpellInputHandler.EquippedSpells[i] == null) continue;

                SpellItemSO spell = Player.LocalClientInstance.SpellInputHandler.EquippedSpells[i];

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
}
