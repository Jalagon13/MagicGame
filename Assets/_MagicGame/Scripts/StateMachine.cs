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

    public BaseState<EState> GetState(EState key)
    {
        if (_states.TryGetValue(key, out var state))
        {
            return state;
        }

        Debug.LogWarning($"State {key} not found in state machine.");
        return null;
    }

    protected virtual void Start()
    {
        _currentState?.EnterState();
    }

    public virtual void UpdateAI()
    {
        if (_currentState == null) return;
        
        _currentState.UpdateAllStates();
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
