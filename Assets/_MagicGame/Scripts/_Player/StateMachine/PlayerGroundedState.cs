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

    protected override void EnterState()
    {
        // Debug.Log("Player entering grounded");

    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        if(_ctx.HeldItem is ToolItemSO && _ctx.SwingCooldownTimer.PercentRemaining <= 0 && GameInput.Instance.GetPrimaryHeldDown() && !Pointer.IsOverUI() && !Pointer.IsOverInteractable())
        {
            SwitchState(AIState.Attacking);
        }
    }

    public override void ExitState()
    {
        
    }
}