using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class StateMachine<EState> : IAIBrain where EState : Enum
{
    protected Dictionary<EState, BaseState<EState>> _states = new();
    protected BaseState<EState> _currentState;
    protected bool _isTransitioningState = false;
	
    protected virtual void Start()
    {
        if(_currentState != null)
        {
            _currentState.EnterState();
        }
    }

    public virtual void UpdateAI()
    {
        if (_currentState == null) return;

        EState nextStateKey = _currentState.GetNextState();

        if (!_isTransitioningState && nextStateKey.Equals(_currentState.StateKey))
        {
            _currentState.FixedUpdate();
        }
        else if (!_isTransitioningState)
        {
            TransitionToState(nextStateKey);
        }
    }
    
    public virtual void Dispose()
    {
        
    }

    public abstract void ReceiveHP(ServerCharacter inflicter, int amount);

    public void TransitionToState(EState statekey)
    {
        _isTransitioningState = true;
        _currentState.ExitState();
        _currentState = _states[statekey];
        _currentState.EnterState();
        _isTransitioningState = false;
    }
}
