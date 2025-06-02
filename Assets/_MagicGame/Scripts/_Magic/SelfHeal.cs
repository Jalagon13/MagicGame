using Unity.Netcode;
using UnityEngine;

public class SelfHeal : Spell
{
    [field: SerializeField] public int HealAmount { get; private set; }
    [field: SerializeField] public ParticleSystem HealVisualVFX { get; private set; }

    protected override void OnSpellSpawned()
    {
        transform.position = Player.LocalClientInstance.transform.position;
    }

    protected override void OnExecuteSpellStart()
    {
        SelfCastStartClientRpc(RpcTarget.Single(SpellData.Value.OwnerPlayerId, RpcTargetUse.Persistent));
    }

    protected override void OnSpellEnd()
    {
        // Optional: cleanup logic
        if (HealVisualVFX != null)
        {
            HealVisualVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    protected override void OnSpellCanceled()
    {
        // Optional: cancel logic
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SelfCastStartClientRpc(RpcParams rpcParams = default)
    {
        // Player.LocalClientInstance.HealthState.HealRpc(HealAmount);
    }
}
