using UnityEngine;

public class BasicNpcKnockbackState : BaseState<BasicNpcStateMachine.BasicNpcState>
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcKnockbackState(BasicNpcStateMachine.BasicNpcState key, StateMachine<BasicNpcStateMachine.BasicNpcState> context) : base(key, context)
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

    public override void FixedUpdate()
    {
        
    }

    public override BasicNpcStateMachine.BasicNpcState GetNextState()
    {
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
        {
            if(_ctx.IsAngry)
            {
                return BasicNpcStateMachine.BasicNpcState.Pursuing;
            }
        
            return BasicNpcStateMachine.BasicNpcState.Idle;
        }

        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
        {
            return BasicNpcStateMachine.BasicNpcState.Moving;
        }
        
        return StateKey;
    }
}