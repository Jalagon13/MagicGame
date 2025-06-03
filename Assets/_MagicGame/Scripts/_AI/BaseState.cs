using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState
{
    protected BaseState _currentSubState;
    public BaseState CurrentSubState => _currentSubState;
    private BaseState _currentSuperState;
    
    private bool _isSuperState = false;
    protected bool IsSuperState { set { _isSuperState = value; } }

    protected StateMachine Context { get; private set; }
    public AIState StateKey { get; private set;}

    public BaseState(AIState key, StateMachine context)
    {
        StateKey = key;
        Context = context;
    }

    public void EnterStateWithNetworkSync()
    {
        if(_isSuperState)
        {
            Context.ServerCharacter.SuperAIState.Value = StateKey;
        }
        else
        {
            Context.ServerCharacter.SubAIState.Value = StateKey;
        }
        
        EnterState();
    }
    
    public virtual void ClientEnterState(){}
    public virtual void ClientUpdateState(){}
    public virtual void ClientExitState(){}

    protected abstract void EnterState();
    public abstract void UpdateState();
    public abstract void CheckSwitchStates();
    public abstract void ExitState();

    protected void SwitchState(AIState state)
    {
        var newState = Context.GetState(state);
        if (newState == this) return;

        ExitState();

        if (_isSuperState)
        {
            Context.TransitionToState(newState.StateKey); // This handles EnterState
        }
        else
        {
            newState.EnterStateWithNetworkSync(); // Only call EnterState directly for substates
            _currentSuperState?.SetSubState(state);
        }
    }

    public void UpdateAllStates()
    {
        UpdateState();
        CheckSwitchStates();
        _currentSubState?.UpdateAllStates();
    }
    
    protected void SetSuperState(BaseState state)
    {
        _currentSuperState = state;
    }
    
    protected void SetSubState(AIState aiState)
    {
        var state = Context.GetState(aiState);

        _currentSubState = state;
        state.SetSuperState(this);
    }
}
