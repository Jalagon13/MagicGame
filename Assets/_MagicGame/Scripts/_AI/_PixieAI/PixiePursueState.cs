using UnityEngine;

public class PixiePursueState : BasicNpcPursueState
{
    private PixieStateMachine _ctx;

    public PixiePursueState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as PixieStateMachine;
    }
}