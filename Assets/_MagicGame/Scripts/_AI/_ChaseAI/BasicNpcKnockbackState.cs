using UnityEngine;

public class BasicNpcKnockbackState : BaseState<AIState>
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcKnockbackState(AIState key, StateMachine<AIState> context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    public override void EnterState()
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
                SwitchState(AIState.Pursuing);
            }

            SwitchState(AIState.Idle);
            
        }

        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
        {
            SwitchState(AIState.Moving);
        }
    }
}