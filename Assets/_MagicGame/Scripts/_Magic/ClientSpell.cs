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

    public override void OnNetworkSpawn()
    {
        if (!IsClient) return;
        
        _serverSpell.SpellStateNV.OnValueChanged += HandleSpellStateChange;
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
}
