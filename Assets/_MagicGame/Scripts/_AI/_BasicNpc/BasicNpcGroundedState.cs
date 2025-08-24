using UnityEngine;

public class BasicNpcGroundedState : BaseState
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcGroundedState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = context as BasicNpcStateMachine;
        IsSuperState = true; // This is a super state
        SetSubState(AIState.Idle); // Default sub state is idle
    }

    protected override void EnterState(AIStateData stateData)
    {
        // Logic for entering the grounded state
    }

    public override void UpdateState()
    {
        // Logic for updating the grounded state
        if(_ctx.ServerCharacter.NetLifeState.LifeState.Value == LifeState.Dead)
        {
            SwitchState(new AIStateData(AIState.Dead, _ctx.ServerCharacter.InflicterToTargetDirection));
        }
    }

    public override void ExitState()
    {
        // Logic for exiting the grounded state
    }

    public override void CheckSwitchStates()
    {
        // Logic to check if we should switch to another state
    }
}