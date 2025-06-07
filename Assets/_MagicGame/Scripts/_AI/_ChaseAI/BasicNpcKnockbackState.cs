using UnityEngine;

public class BasicNpcKnockbackState : BaseState
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcKnockbackState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    protected override void EnterState()
    {
        Debug.Log($"Knockback state");
    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
        {
            if (_ctx.IsAngry)
            {
                SwitchState(new AIStateData(AIState.Pursuing));
            }

            SwitchState(new AIStateData(AIState.Idle));
            
        }

        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
        {
            SwitchState(new AIStateData(AIState.Moving));
        }
    }
}