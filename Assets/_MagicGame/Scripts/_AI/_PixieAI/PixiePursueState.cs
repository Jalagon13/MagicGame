using UnityEngine;

public class PixiePursueState : BasicNpcPursueState
{
    private PixieStateMachine _ctx;

    public PixiePursueState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PixieStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log($"Pixie Pursue state enter");
        base.EnterState(stateData);
    }

    public override void CheckSwitchStates()
    {
        // Custom logic for Pixie Charging Dash State
    
        base.CheckSwitchStates();
    }
}