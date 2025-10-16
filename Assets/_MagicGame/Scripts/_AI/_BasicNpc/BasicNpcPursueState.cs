using UnityEngine;


namespace ProjectTinker
{
	public class BasicNpcPursueState : BaseState
	{
	    private BasicNpcStateMachine _ctx;

	    public BasicNpcPursueState(AIState key, StateMachine context) : base(key, context)
	    {
	        _ctx = Context as BasicNpcStateMachine;
	    }

	    protected override void EnterState(AIStateData stateData)
	    {
	        // Debug.Log($"Basic NPC Pursuing state");

	        _ctx.ServerCharacter.Movement.StartPursue(_ctx.PursueTargetTransform);
	    }

	    public override void UpdateState()
	    {
	        // Strafe while chasing
	        if (_ctx.IsStrafing && _ctx.PursueTargetTransform != null)
	        {
	            // Instead of using Movement.DesiredDirection:
	            Vector2 targetDir = ((Vector2)_ctx.PursueTargetTransform.position -
	                                 (Vector2)_ctx.ServerCharacter.transform.position).normalized;

	            Vector2 perpendicular = Vector2.zero;
	            if (targetDir != Vector2.zero)
	            {
	                perpendicular = new Vector2(-targetDir.y, targetDir.x).normalized * _ctx.StrafingDirection;
	            }
	            _ctx.ServerCharacter.Movement.SetPursueOffset(perpendicular, _ctx.ServerCharacter.Data.StrafeSpeedMultiplier);
	        }
	        else
	        {
	            _ctx.ServerCharacter.Movement.SetPursueOffset(Vector2.zero, 1);
	        }
	    }

	    public override void CheckSwitchStates()
	    {
	        if (_ctx.ServerCharacter.MovementState.Value == MovementState.Knockback)
	        {
	            _ctx.ServerCharacter.Movement.SetPursueOffset(Vector2.zero, 1);
	            SwitchState(new AIStateData(AIState.Knockbacked));
	            return;
	        }
	        else if (!_ctx.IsPursuingPlayerOrBreadCrumb)
	        {
	            _ctx.ServerCharacter.Movement.SetPursueOffset(Vector2.zero, 1);
	            SwitchState(new AIStateData(AIState.Idle));
	            return;
	        }
	    }

	    public override void ExitState()
	    {
	        _ctx.ServerCharacter.Movement.SetPursueOffset(Vector2.zero, 1);
	    }
	}
}