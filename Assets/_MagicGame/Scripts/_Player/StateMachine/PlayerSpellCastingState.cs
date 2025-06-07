using UnityEngine;

public class PlayerSpellCastingState : BaseState
{
    private PlayerStateMachine _ctx;
    private GameObject _clientChargeVfx;

    public PlayerSpellCastingState(AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState()
    {
        Debug.Log($"OnClient Player entering spell casting");
        // Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(HasteMultiplier);
    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        if(!SpellManager.Instance.IsCasting)
        {
            SwitchState(new AIStateData(AIState.Grounded));
        }
    }

    public override void ExitState()
    {
        // Player.LocalClientInstance.PlayerStats.ApplySpeedModifier(1f);
        // Player.LocalClientInstance.PlayerKnockback.ApplyKnockback(ActionManager.MouseWorldPosition, 0, _loadedSpell.SpellToCast.Recoil);
    }

    public override void ClientEnterState(AIStateData stateData)
    {
        Debug.Log($"Playing on client{_ctx.ServerCharacter.NetworkManager.LocalClientId}, GO Id this state belongs to: {_ctx.ServerCharacter.OwnerClientId}, SpellId for this GO: {stateData.Amount}");
        SpellItemSO spellToCast = GameManager.Instance.GetItemSOFromItemId(stateData.Amount) as SpellItemSO;
        // Debug.Log($"OnClient Spell id: {_ctx.ServerCharacter.CurrentSpellId.Value} {_ctx.ServerCharacter.gameObject.name}, {_ctx.ServerCharacter.OwnerClientId}");
        // Debug.Log($"OnCLient Spell to cast: {spellToCast.Name}");

        _clientChargeVfx = Object.Instantiate(spellToCast.ChargeVFX, _ctx.PlayerRef.PlayerHand.SpellSpawnTransform);
        _clientChargeVfx.transform.localPosition = Vector3.zero;
        _clientChargeVfx.GetComponent<MagicCircle>().StartAnimation(spellToCast.CastTime);
    }

    public override void ClientUpdateState(AIStateData stateData)
    {
        
    }
    
    public override void ClientExitState(AIStateData stateData)
    {
        Debug.Log($"OnClient exiting spell casting");
        _clientChargeVfx?.GetComponent<MagicCircle>().StopAnimation();
    }
}