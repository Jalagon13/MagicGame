using Unity.Netcode;
using UnityEngine;

public class SelfCastDash : Spell
{
    [SerializeField] private float _bouncePower = 30f;

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

        Vector2 knockerSourcePosition = (Vector2)Player.LocalClientInstance.transform.position + Player.LocalClientInstance.StateMachine.MoveVector;
        Player.LocalClientInstance.PlayerKnockback.ApplyKnockback(knockerSourcePosition, 0, _bouncePower, true);

        _isSelfCasting = true;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        // Only play for the spell caster client, is started, and self casting started
        if (!Started.Value || !_isSelfCasting || Player.LocalClientInstance.OwnerClientId != SpellData.Value.OwnerPlayerId) return;
    }

    private void SelfCastEnd(bool previousValue, bool newValue)
    {
        if (newValue) return;
    }
}
