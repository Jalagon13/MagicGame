using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SelfCastSpeed : Spell
{
    [SerializeField] private float _playerSpeedDecayMult = 2f;
    
    private bool _isSelfCasting = false;

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);
        
        SelfCastStartClientRpc(RpcTarget.Single(SpellData.Value.OwnerPlayerId, RpcTargetUse.Persistent));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SelfCastStartClientRpc(RpcParams rpcParams = default)
    {
        Started.OnValueChanged += SelfCastEnd;

        PlayerStats.Instance.ApplySpeedModifier(_playerSpeedDecayMult);

        _isSelfCasting = true;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        // Only play for the spell caster client, is started, and self casting started
        if(!Started.Value || !_isSelfCasting || Player.LocalClientInstance.OwnerClientId != SpellData.Value.OwnerPlayerId) return;

        PlayerStats.Instance.ApplySpeedModifier(Mathf.Lerp(_playerSpeedDecayMult, 1, SpellLifeTimer.PercentRemaining));
    }

    private void SelfCastEnd(bool previousValue, bool newValue)
    {
        if(newValue) return;
        
        PlayerStats.Instance.ApplySpeedModifier(1);
    }
}
