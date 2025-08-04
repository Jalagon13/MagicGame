using UnityEngine;

public class PixieStateMachine : StateMachine
{
    

    public PixieStateMachine(ServerCharacter serverCharacter)
    {
        _serverCharacter = serverCharacter;

        // Sub States
        _states[AIState.Idle] = new BasicNpcIdleState(AIState.Idle, this);
        _states[AIState.Moving] = new BasicNpcMoveState(AIState.Moving, this);
        _states[AIState.Knockbacked] = new BasicNpcKnockbackState(AIState.Knockbacked, this);
        _states[AIState.Pursuing] = new BasicNpcPursueState(AIState.Pursuing, this);

        // Super States
        _states[AIState.Grounded] = new BasicNpcGroundedState(AIState.Grounded, this);
        _states[AIState.Dead] = new BasicNpcDeadState(AIState.Dead, this);
        
        // Unique Pixie Super States
        _states[AIState.SpellCasting] = new PixieChargingDashState(AIState.SpellCasting, this);
        _states[AIState.Attacking] = new PixieDashState(AIState.Attacking, this);

        // Start on the Grounded State
        _currentState = _states[AIState.Grounded];
    }

    public override void ReceiveHP(ServerCharacter inflicter, int amount)
    {
        
    }
}
