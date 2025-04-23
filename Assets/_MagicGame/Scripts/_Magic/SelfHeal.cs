using Unity.Netcode;
using UnityEngine;

public class SelfHeal : Spell
{
    [field: SerializeField] public int HealAmount { get; private set; }
    [field: SerializeField] public ParticleSystem HealVisualVFX { get; private set; }

    protected override void OnOwnerExecuteSpellStart()
    {
        SelfCastStartClientRpc(RpcTarget.Single(SpellData.Value.OwnerPlayerId, RpcTargetUse.Persistent));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SelfCastStartClientRpc(RpcParams rpcParams = default)
    {
        Player.LocalClientInstance.HealthState.HealRpc(HealAmount);
    }

    protected override void Update()
    {
        base.Update();
    
        if (IsOwner)
        {
            transform.position = Player.LocalClientInstance.transform.position;
        }
    }

    protected override void OnStopped()
    {
        if (HealVisualVFX != null)
        {
            HealVisualVFX.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
