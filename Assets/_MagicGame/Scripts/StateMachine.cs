using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public enum AIState
{
    None,

    // Root States
    Grounded,
    Attacking,
    SpellCasting,
    
    // Sub States
    Idle,
    Moving,
    Knockbacked,
    Pursuing,
}

public abstract class StateMachine
{
    protected Dictionary<AIState, BaseState> _states = new();
    protected BaseState _currentState;
    protected bool _isTransitioningState = false;

    protected ServerCharacter _serverCharacter;
    public ServerCharacter ServerCharacter => _serverCharacter;
    public CharacterDataSO CharacterData => _serverCharacter.Data;

    public BaseState GetState(AIState key)
    {
        if (_states.TryGetValue(key, out var state))
        {
            return state;
        }

        Debug.LogWarning($"State {key} not found in state machine.");
        return null;
    }

    public void StartStateMachine()
    {
        _currentState?.EnterStateWithNetworkSync();
        _currentState.CurrentSubState?.EnterStateWithNetworkSync();
    }

    public virtual void UpdateAI()
    {
        if (_currentState == null) return;
        
        _currentState.UpdateAllStates();
    }
    
    public virtual void OwnerInitialization() { }
    public virtual void Dispose() { }

    public abstract void ReceiveHP(ServerCharacter inflicter, int amount);

    public void TransitionToState(AIState statekey)
    {
        _isTransitioningState = true;
        _currentState.ExitState();
        _currentState = _states[statekey];
        _currentState.EnterStateWithNetworkSync();
        _isTransitioningState = false;
    }
}
