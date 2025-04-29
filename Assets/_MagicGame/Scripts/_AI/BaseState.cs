using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState<EState> where EState : Enum
{
    public BaseState(EState key, StateMachine<EState> context)
    {
        StateKey = key;
        Context = context;
    }
	
    protected StateMachine<EState> Context { get; private set; }
    public EState StateKey { get; private set;}

    public abstract void EnterState();
    public abstract void ExitState();
    public abstract void FixedUpdate();
    public abstract EState GetNextState();
}
