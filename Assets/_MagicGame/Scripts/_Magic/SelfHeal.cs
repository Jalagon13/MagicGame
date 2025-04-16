using Unity.Netcode;
using UnityEngine;

public class SelfHeal : Spell
{
    [field: SerializeField] public int HealAmount { get; private set; }

    private bool _isSelfCasting = false;

    public override void ExecuteSpellStart(Vector2 finalDirection, Vector2 spawnPoint)
    {
        base.ExecuteSpellStart(finalDirection, spawnPoint);
        
        SelfCastStartClientRpc(RpcTarget.Single(SpellData.Value.OwnerPlayerId, RpcTargetUse.Persistent));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SelfCastStartClientRpc(RpcParams rpcParams = default)
    {
        Player.LocalClientInstance.HealthState.HealRpc(HealAmount);

        _isSelfCasting = true;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Only play for the spell caster client, is started, and self casting started
        if (!Started.Value || !_isSelfCasting || Player.LocalClientInstance.OwnerClientId != SpellData.Value.OwnerPlayerId) return;
    }
}
