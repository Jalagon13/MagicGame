using System;
using UnityEngine;

public class BasicNpcFleeState : BaseState
{
    private BasicNpcStateMachine _ctx;
    private Vector2 _fleeDirection;
    private Timer _fleeTimer;
    private bool _fleeDone;

    public BasicNpcFleeState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        // Debug.Log($"Flee State");
        _fleeDone = false;
        _fleeTimer = new Timer(_ctx.CharacterData.FleeDuration);
        _fleeTimer.OnTimerEnd += OnFleeEnd;
        
        _fleeDirection = _ctx.ServerCharacter.transform.position - _ctx.ServerCharacter.Inflicter.transform.position;
        _fleeDirection.Normalize();
        
        _ctx.ServerCharacter.Movement.StartFlee(_fleeDirection);
    }

    private void OnFleeEnd(object sender, EventArgs e)
    {
        _fleeTimer.OnTimerEnd -= OnFleeEnd;
        _fleeDone = true;

        // Debug.Log("Fleeing done");
    }

    public override void ExitState()
    {
        _fleeTimer.OnTimerEnd -= OnFleeEnd;
    }

    public override void UpdateState()
    {
        _fleeTimer.Tick(Time.deltaTime);
    }

    public override void CheckSwitchStates()
    {
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            SwitchState(new AIStateData(AIState.Knockbacked));
        }
        else if (_fleeDone)
        {
            SwitchState(new AIStateData(AIState.Idle));
        }
        
    }
}