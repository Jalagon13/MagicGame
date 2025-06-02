using UnityEngine;

public class PlayerGroundedState : BaseState<AIState>
{
    private PlayerStateMachine _ctx;

    public PlayerGroundedState(AIState key, StateMachine<AIState> context) : base(key, context)
    {
        _ctx = Context as PlayerStateMachine;
        IsRootState = true;
        SetSubState(AIState.Idle);
    }

    public override void EnterState()
    {
        Debug.Log("Player entering grounded");

        _currentSubState.EnterState();
    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        
    }

    public override void ExitState()
    {
        
    }
}