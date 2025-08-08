using UnityEngine;

public class BasicNpcPursueState : BaseState
{
    private BasicNpcStateMachine _ctx;
    private Timer _setDirectionTimer;

    public BasicNpcPursueState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log($"Basic NPC Pursuing state");

        _ctx.ServerCharacter.Movement.StartPursue(_ctx.PursueTargetTransform);
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
        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
        {
            SwitchState(new AIStateData(AIState.Knockbacked));
            return;
        }
        else if (!_ctx.IsPursuingPlayerOrBreadCrumb)
        {
            SwitchState(new AIStateData(AIState.Idle));
            return;
        }
    }

    public override void ExitState()
    {
        
    }
}