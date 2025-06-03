using System.Collections;
using UnityEngine;

public class PlayerAttackState : BaseState
{
    private PlayerStateMachine _ctx;
    private Timer _swingTimer;

    public PlayerAttackState(AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState()
    {
        Debug.Log("Player entering swing");
        _swingTimer = new(1f);
    }

    public override void UpdateState()
    {
        _swingTimer.Tick(Time.deltaTime);
    }

    public override void CheckSwitchStates()
    {
        if (_swingTimer.RemainingSeconds <= 0)
        {
            SwitchState(AIState.Grounded);
        }
    }

    public override void ExitState()
    {
        
    }
}
