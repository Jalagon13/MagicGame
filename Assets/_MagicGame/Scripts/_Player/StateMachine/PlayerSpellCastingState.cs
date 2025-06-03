using UnityEngine;

public class PlayerSpellCastingState : BaseState
{
    private PlayerStateMachine _ctx;

    public PlayerSpellCastingState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState()
    {
        Debug.Log($"Player entering spell casting");
    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        
    }

    public override void ExitState()
    {
        
    }
}