using Unity.Netcode;
using UnityEngine;

public class SelfHeal : Spell
{
    [field: SerializeField] public int HealAmount { get; private set; }

    private bool _isSelfCasting = false;

    protected override void OnOwnerExecuteSpellStart()
    {
        SelfCastStartClientRpc(RpcTarget.Single(SpellData.Value.OwnerPlayerId, RpcTargetUse.Persistent));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SelfCastStartClientRpc(RpcParams rpcParams = default)
    {
        Player.LocalClientInstance.HealthState.HealRpc(HealAmount);

        _isSelfCasting = true;
    }
}
