using UnityEngine;

public class PixieKnockbackState : BasicNpcKnockbackState
{
    private PixieStateMachine _ctx;

    public PixieKnockbackState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PixieStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        // Initialize movement parameters or animations here
        // BUG: This played twice why??
        Debug.Log($"Pixie Knockback state");
    }

    public override void CheckSwitchStates()
    {
        // Logic to switch to other states if conditions are met
        base.CheckSwitchStates();
    }
}