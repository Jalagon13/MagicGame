using System.Collections;
using UnityEngine;

public class PlayerAttackState : BaseState
{
    private PlayerStateMachine _ctx;
    private Timer _tempTImer;
    private float _swingCd;

    public PlayerAttackState(AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState()
    {
        Debug.Log("Player entering swing");
        _swingCd = (_ctx.HeldItem as ToolItemSO).SwingCooldown;
        _tempTImer = new((_ctx.HeldItem as ToolItemSO).SwingDuration);
    }

    public override void UpdateState()
    {
        _tempTImer.Tick(Time.deltaTime);
    }

    public override void CheckSwitchStates()
    {
        if (_tempTImer.RemainingSeconds <= 0)
        {
            SwitchState(AIState.Grounded);
        }
    }

    public override void ExitState()
    {
        _ctx.SwingCooldownTimer.AddTime(_swingCd);
    }
}
