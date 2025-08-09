using UnityEngine;

public class PlayerDeadState : BaseState
{
    private PlayerStateMachine _ctx;
    
    public PlayerDeadState (AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log("Player entering dead");
        if (_ctx.ServerCharacter.TryGetComponent(out Collider2D collider2D))
        {
            collider2D.enabled = false;
        }
    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        if(_ctx.ServerCharacter.LifeState == LifeState.Alive)
        {
            SwitchState(new AIStateData(AIState.Grounded));
        }
    }

    public override void ExitState()
    {
        
    }

    public override void ClientEnterState(AIStateData stateData)
    {
        // NTFS: Player death animations here, just turn off visuals for now
        _ctx.ServerCharacter.ClientFeedbacks.RotateGibs(stateData.Payload);
        _ctx.ServerCharacter.ClientFeedbacks.PlayDeathFeedbacksRpc();

    }
    
    public override void ClientExitState(AIStateData stateData)
    {
        _ctx.ServerCharacter.ClientCharacter.Visuals.SetActive(true);
    }
}