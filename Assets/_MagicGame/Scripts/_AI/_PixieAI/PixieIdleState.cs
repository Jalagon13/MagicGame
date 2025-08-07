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
        // TODO: Add Pixie-specific state switching logic here
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            SwitchState(new AIStateData(AIState.Knockbacked));
            return;
        }
        else if (_ctx.IsPursuingPlayerOrBreadCrumb)
        {
            SwitchState(new AIStateData(AIState.Pursuing));
            return;
        }

        base.CheckSwitchStates();
    }
}