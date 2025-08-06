using UnityEngine;

public class PixieIdleState : BasicNpcIdleState
{
    private PixieStateMachine _ctx;

    public PixieIdleState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PixieStateMachine;
    }

    public override void CheckSwitchStates()
    {
        // Logic to switch to other states if conditions are met
        base.CheckSwitchStates();

        // TODO: Add Pixie-specific state switching logic here
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            SwitchState(new AIStateData(AIState.Knockbacked));
            return;
        }
    }
}