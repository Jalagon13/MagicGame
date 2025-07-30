using UnityEngine;

public class BasicNpcGroundedState : BaseState
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcGroundedState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = context as BasicNpcStateMachine;
        IsSuperState = true; // This is a super state
        SetSubState(AIState.Idle); // Default sub state is Idle
    }

    protected override void EnterState(AIStateData stateData)
    {
        // Logic for entering the grounded state
    }

    public override void UpdateState()
    {
        // Logic for updating the grounded state
    }

    public override void ExitState()
    {
        // Logic for exiting the grounded state
        Debug.Log("Exiting Grounded State");
    }

    public override void CheckSwitchStates()
    {
        // Logic to check if we should switch to another state
    }
}