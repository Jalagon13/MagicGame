using System;
using UnityEngine;

public class BasicNpcIdleState : BaseState<BasicNpcStateMachine.BasicNpcState>
{
    private BasicNpcStateMachine _ctx;
    private Timer _idleTimer;
    private bool _idleComplete;

    public BasicNpcIdleState(BasicNpcStateMachine.BasicNpcState key, StateMachine<BasicNpcStateMachine.BasicNpcState> context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    public override void EnterState()
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

    public override void FixedUpdate()
    {
        _idleTimer.Tick(Time.fixedDeltaTime);
    }

    public override BasicNpcStateMachine.BasicNpcState GetNextState()
    {
        if(_idleComplete)
        {
            return BasicNpcStateMachine.BasicNpcState.Moving;
        }
        
        if(_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            return BasicNpcStateMachine.BasicNpcState.Knockback;
        }
        
        if(_ctx.IsChasing)
        {
            return BasicNpcStateMachine.BasicNpcState.Pursuing;
        }

        return StateKey;
    }

    private void IdleDone(object sender, EventArgs e)
    {
        _idleTimer.OnTimerEnd -= IdleDone;
        _idleComplete = true;
    }
}