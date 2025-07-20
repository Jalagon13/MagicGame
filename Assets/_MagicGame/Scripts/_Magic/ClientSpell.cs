using System;
using Unity.Netcode;
using UnityEngine;

public class ClientSpell : NetworkBehaviour
{
    [SerializeField] 
    private ServerSpell _serverSpell;
    
    [SerializeField] 
    private GameObject _visualization;
    public GameObject Visualization => _visualization;
    
    [SerializeField] 
    private GameObject _modifiersContainer;

    public override void OnNetworkSpawn()
    {
        if (!IsClient) return;
        
        _serverSpell.SpellStateNV.OnValueChanged += HandleSpellStateChange;
        _serverSpell.SpellData.OnValueChanged += OnSpellDataInitialized;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsClient) return;

        _serverSpell.SpellStateNV.OnValueChanged -= HandleSpellStateChange;
    }

    private void Update()
    {
        if (!IsClient) return;

        if(_serverSpell.SpellStateNV.Value == SpellState.Casting)
        {
            _serverSpell?.ClientSpellUpdate(this);
        }
    }

    private void HandleSpellStateChange(SpellState previousValue, SpellState newValue)
    {
        if(previousValue == SpellState.Charging && newValue == SpellState.Casting)
        {
            _serverSpell.ClientSpellStart(this);
        }
        else if(previousValue == SpellState.Casting && newValue == SpellState.Stopping)
        {
            _serverSpell.ClientSpellStop(this);
        }
    }

    private void OnSpellDataInitialized(SyncSpellData oldData, SyncSpellData newData)
    {
        _serverSpell.SpellData.OnValueChanged -= OnSpellDataInitialized;
        Debug.Log($"spell mod amount: {newData.SpellMods.Count}");
        foreach (var item in newData.SpellMods)
        {
            if (GameManager.Instance.GetItemSOFromItemId(item) is SpellModItemSO spellMod)
            {
                Debug.Log($"[ClientSpell] Applying visual mod: {spellMod.Name}");
                // TODO: Add any visual effect or logic specific to this mod here
                Instantiate(spellMod.SpellModPrefab, _modifiersContainer.transform);
            }
        }
    }
}
