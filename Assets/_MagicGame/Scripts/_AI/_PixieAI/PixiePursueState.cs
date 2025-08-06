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
        // Initialize movement parameters or animations here
        Debug.Log($"Pixie Pursue State");
    }

    public override void CheckSwitchStates()
    {
        // Logic to switch to other states if conditions are met
        // Custom logic for the dash state
    }
}