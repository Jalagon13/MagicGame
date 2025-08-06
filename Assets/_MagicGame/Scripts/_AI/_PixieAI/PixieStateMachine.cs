using UnityEngine;

public class PixieStateMachine : BasicNpcStateMachine
{
    public PixieStateMachine(ServerCharacter serverCharacter) : base(serverCharacter)
    {
        _serverCharacter = serverCharacter;

        // Sub States
        _states[AIState.Idle] = new PixieIdleState(AIState.Idle, this);
        _states[AIState.Moving] = new PixieMoveState(AIState.Moving, this);
        _states[AIState.Knockbacked] = new PixieKnockbackState(AIState.Knockbacked, this);
        _states[AIState.Pursuing] = new PixiePursueState(AIState.Pursuing, this);

        // Unique Pixie Sub States
        _states[AIState.SpellCasting] = new PixieChargingDashState(AIState.SpellCasting, this);
        _states[AIState.Attacking] = new PixieDashState(AIState.Attacking, this);

        // Super States
        _states[AIState.Grounded] = new BasicNpcGroundedState(AIState.Grounded, this);
        _states[AIState.Dead] = new BasicNpcDeadState(AIState.Dead, this);
        
        // Start on the Grounded State
        _currentState = _states[AIState.Grounded];
    }

    public override void ReceiveHP(ServerCharacter inflicter, int amount)
    {
        
    }
}
