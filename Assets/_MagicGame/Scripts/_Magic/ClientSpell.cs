using System;
using Unity.Netcode;
using UnityEngine;

public class ClientSpell : NetworkBehaviour
{
    [SerializeField] 
    private ServerSpell _serverSpell;

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
            _serverSpell?.ClientSpellUpdate();
        }
    }

    private void HandleSpellStateChange(SpellState previousValue, SpellState newValue)
    {
        if(previousValue == SpellState.Charging && newValue == SpellState.Casting)
        {
            _serverSpell.ClientSpellStart();
        }
        else if(previousValue == SpellState.Casting && newValue == SpellState.Stopping)
        {
            _serverSpell.ClientSpellStop();
        }
    }
}
