using System;
using UnityEngine;

public class BasicNpcIdleState : BaseState
{
    private BasicNpcStateMachine _ctx;
    private Timer _idleTimer;
    private bool _idleComplete;

    public BasicNpcIdleState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    protected override void EnterState()
    {  
        Debug.Log($"Idle State");
        _idleComplete = false;
        
        float idleDuration = UnityEngine.Random.Range(_ctx.CharacterData.MinIdleDuration, _ctx.CharacterData.MaxIdleDuration);
        
        if(idleDuration <= 0)
        {
            idleDuration = 0.0001f;
        } 
        
        _idleTimer = new(idleDuration);
        _idleTimer.OnTimerEnd += IdleDone;
        
        _ctx.ServerCharacter.Movement.StartIdle();
    }

    public override void ExitState()
    {
        _idleTimer.OnTimerEnd -= IdleDone;
    }

    public override void UpdateState()
    {
        _idleTimer.Tick(Time.fixedDeltaTime);
    }

    public override void CheckSwitchStates()
    {
        if (_idleComplete)
        {
            SwitchState(new AIStateData(AIState.Moving));
        }

        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            SwitchState(new AIStateData(AIState.Knockbacked));
        }

        if (_ctx.IsChasing)
        {
            SwitchState(new AIStateData(AIState.Pursuing));
        }
    }

    private void IdleDone(object sender, EventArgs e)
    {
        _idleTimer.OnTimerEnd -= IdleDone;
        _idleComplete = true;
    }
}