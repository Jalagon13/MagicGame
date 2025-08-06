using UnityEngine;

public class PixieKnockbackState : BaseState
{
    private PixieStateMachine _ctx;

    public PixieKnockbackState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PixieStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        // Initialize movement parameters or animations here
    }

    public override void UpdateState()
    {
        // Handle movement logic here
    }

    public override void CheckSwitchStates()
    {
        // Logic to switch to other states if conditions are met
    }

    public override void ExitState()
    {
        // Cleanup or reset parameters when exiting this state
    }
}