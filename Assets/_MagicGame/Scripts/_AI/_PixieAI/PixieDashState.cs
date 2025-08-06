using UnityEngine;

public class PixieDashState : BaseState
{
    private PixieStateMachine _ctx;

    public PixieDashState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PixieStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {

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