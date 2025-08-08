using UnityEngine;

public class PixieStateMachine : BasicNpcStateMachine
{
    public PixieStateMachine(ServerCharacter serverCharacter) : base(serverCharacter)
    {
        _serverCharacter = serverCharacter;

        // Sub States
        _states[AIState.Idle] = new BasicNpcIdleState(AIState.Idle, this);
        _states[AIState.Moving] = new BasicNpcMoveState(AIState.Moving, this);
        _states[AIState.Knockbacked] = new BasicNpcKnockbackState(AIState.Knockbacked, this);
        
        // Pixie Specific Sub States
        _states[AIState.Pursuing] = new PixiePursueState(AIState.Pursuing, this);
        _states[AIState.SpellCasting] = new PixieChargingDashState(AIState.SpellCasting, this);
        _states[AIState.Attacking] = new PixieDashState(AIState.Attacking, this);

        // Super States
        _states[AIState.Grounded] = new BasicNpcGroundedState(AIState.Grounded, this);
        _states[AIState.Dead] = new BasicNpcDeadState(AIState.Dead, this);
        
        // Start on the Grounded State
        _currentState = _states[AIState.Grounded];
    }
}
