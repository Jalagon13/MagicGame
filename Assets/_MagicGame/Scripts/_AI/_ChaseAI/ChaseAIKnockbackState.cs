using UnityEngine;

public class ChaseAIKnockbackState : BaseState<ChaseAIStateMachine.ChaseAIState>
{
    private ChaseAIStateMachine _ctx;

    public ChaseAIKnockbackState(ChaseAIStateMachine.ChaseAIState key, StateMachine<ChaseAIStateMachine.ChaseAIState> context) : base(key, context)
    {
        _ctx = Context as ChaseAIStateMachine;
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

    public override ChaseAIStateMachine.ChaseAIState GetNextState()
    {
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
        {
            if(_ctx.IsAngry)
            {
                return ChaseAIStateMachine.ChaseAIState.Pursuing;
            }
        
            return ChaseAIStateMachine.ChaseAIState.Idle;
        }

        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
        {
            return ChaseAIStateMachine.ChaseAIState.Moving;
        }
        
        return StateKey;
    }
}