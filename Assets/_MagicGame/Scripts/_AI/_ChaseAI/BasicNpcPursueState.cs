using UnityEngine;

public class BasicNpcPursueState : BaseState<AIState>
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcPursueState(AIState key, StateMachine<AIState> context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    public override void EnterState()
    {
        Debug.Log($"Pursuing state");

        Vector2 direction = (_ctx.PursueDestination.Value - (Vector2)_ctx.ServerCharacter.transform.position).normalized;
        _ctx.ServerCharacter.Movement.StartPursue(direction);
    }

    public override void ExitState()
    {
        
    }

    public override void UpdateState()
    {
        // Strafe while chasing
        if (_ctx.IsStrafing)
        {
            Vector2 desiredDirection = _ctx.ServerCharacter.Movement.DesiredDirection.normalized;

            // Get a perpendicular vector (left or right)
            Vector2 perpendicular = new Vector2(-desiredDirection.y, desiredDirection.x) * _ctx.StrafingDirection;

            // Apply strafing effect by blending it into the desired direction
            desiredDirection += perpendicular * _ctx.CharacterData.StrafeIntensity;
            _ctx.ServerCharacter.Movement.SetDesiredDirection(desiredDirection.normalized); // Normalize to maintain consistent speed
        }
    }

    public override void CheckSwitchStates()
    {
        if (!_ctx.IsChasing)
        {
            SwitchState(AIState.Idle);
        }
    }
}