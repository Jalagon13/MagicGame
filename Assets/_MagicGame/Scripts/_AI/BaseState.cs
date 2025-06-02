using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState<EState> where EState : Enum
{
    protected BaseState<EState> _currentSubState;
    private BaseState<EState> _currentSuperState;
    
    private bool _isRootState = false;
    protected bool IsRootState { set { _isRootState = value; } }

    protected StateMachine<EState> Context { get; private set; }
    public EState StateKey { get; private set;}
    
    public BaseState(EState key, StateMachine<EState> context)
    {
        StateKey = key;
        Context = context;
    }

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void UpdateState();
    public abstract void CheckSwitchStates();

    protected void SwitchState(EState state)
    {
        var newState = Context.GetState(state);
        
        if(newState == this) return;
        
        Debug.Log($"Switching from {StateKey} to {newState.StateKey}");
    
        ExitState();
        newState.EnterState();

        if (_isRootState)
        {
            Context.TransitionToState(newState.StateKey);
        }
        else
        {
            _currentSuperState?.SetSubState(state);
        }
    }

    public void UpdateAllStates()
    {
        UpdateState();
        CheckSwitchStates();
        _currentSubState?.UpdateAllStates();
    }
    
    protected void SetSuperState(BaseState<EState> state)
    {
        _currentSuperState = state;
    }
    
    protected void SetSubState(EState aiState)
    {
        var state = Context.GetState(aiState);

        _currentSubState = state;
        state.SetSuperState(this);
        Debug.Log($"Setting sub state of {StateKey} to {state.StateKey}");
    }
}
