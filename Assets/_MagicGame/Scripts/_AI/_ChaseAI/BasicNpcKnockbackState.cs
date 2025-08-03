using UnityEngine;

public class BasicNpcKnockbackState : BaseState
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcKnockbackState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log($"Knockback State");
        Vector2 knockbackDirection = _ctx.ServerCharacter.Inflicter != null
            ? (_ctx.ServerCharacter.transform.position - _ctx.ServerCharacter.Inflicter.transform.position).normalized
            : Vector2.zero;

        _ctx.ServerCharacter.Movement.SetDesiredDirection(knockbackDirection);
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
            else if (_ctx.CharacterData.WillFleeWhenProvoked)
            {
                SwitchState(new AIStateData(AIState.Fleeing));
            }
            else
            {
                SwitchState(new AIStateData(AIState.Idle));
            }
        }
        else if (_ctx.ServerCharacter.MovementState.Value == MovementState.Moving)
        {
            if(_ctx.CharacterData.WillFleeWhenProvoked)
            {
                SwitchState(new AIStateData(AIState.Fleeing));
            }
            else
            {
                SwitchState(new AIStateData(AIState.Moving));
            }
        }
    }
}