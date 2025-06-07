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

    public void EnterStateWithNetworkSync(AIStateData stateData)
    {
        EnterState();
        
        if(_isSuperState)
        {
            Context.ServerCharacter.SuperAIState.Value = stateData;
        }
        else
        {
            Context.ServerCharacter.SubAIState.Value = stateData;
        }
    }
    
    public virtual void ClientEnterState(AIStateData stateData) {}
    public virtual void ClientUpdateState(AIStateData stateData) {}
    public virtual void ClientExitState(AIStateData stateData){}

    protected abstract void EnterState();
    public abstract void UpdateState();
    public abstract void CheckSwitchStates();
    public abstract void ExitState();

    protected void SwitchState(AIStateData stateData)
    {
        var newState = Context.GetState(stateData.CurrentState);
        if (newState == this) return;

        ExitState();

        if (_isSuperState)
        {
            Context.TransitionToState(stateData); // This handles EnterState
        }
        else
        {
            newState.EnterStateWithNetworkSync(stateData); // Only call EnterState directly for substates
            _currentSuperState?.SetSubState(stateData.CurrentState);
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
