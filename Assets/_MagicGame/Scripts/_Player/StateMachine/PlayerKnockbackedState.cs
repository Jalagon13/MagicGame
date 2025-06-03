using UnityEngine;

public class PlayerKnockbackedState : BaseState
{
    private PlayerStateMachine _ctx;

	public PlayerKnockbackedState(AIState key, StateMachine context) : base(key, context)
	{
        _ctx = Context as PlayerStateMachine;
	}

    protected override void EnterState()
    {
        Debug.Log("Player entering knockbacked");
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
