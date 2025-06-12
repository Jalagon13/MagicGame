using UnityEngine;

public class PlayerGroundedState : BaseState
{
    private PlayerStateMachine _ctx;

    public PlayerGroundedState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PlayerStateMachine;
        IsSuperState = true;
        SetSubState(AIState.Idle);
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log("Player entering grounded");

    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        if(_ctx.HeldItem is ToolItemSO && _ctx.SwingCooldownTimer.PercentRemaining <= 0 && GameInput.Instance.GetPrimaryHeldDown() && !Pointer.IsOverUI() && !Pointer.IsOverInteractable())
        {
            SwitchState(new AIStateData(AIState.Attacking));
        }
        else if(_ctx.SpellCaster.IsCasting)
        {
            SwitchState(new AIStateData(AIState.SpellCasting, _ctx.SpellCaster.CurrentSpellData.SpellItemId));
        }
        else if(_ctx.ServerCharacter.LifeState == LifeState.Dead)
        {
            SwitchState(new AIStateData(AIState.Dead));
        }
    }

    public override void ExitState()
    {
        
    }
}