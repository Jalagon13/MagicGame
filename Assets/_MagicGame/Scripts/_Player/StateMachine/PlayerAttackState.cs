using System;
using System.Collections;
using UnityEngine;

public class PlayerAttackState : BaseState
{
    private PlayerStateMachine _ctx;
    private float _swingCd;
    private ToolItemSO _toolItemSO;
    private CardinalDirection _swingDirection;

    public PlayerAttackState(AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState()
    {
        // Debug.Log("Player entering swing");
        _toolItemSO = _ctx.HeldItem as ToolItemSO;
        _swingCd = _toolItemSO.SwingCooldown;
        _swingDirection = _ctx.PlayerRef.PlayerHand.AimDirection.Value;
        
        float duration = _toolItemSO.SwingDuration;

        switch (_swingDirection)
        {
            case CardinalDirection.North:
                Swing(150, 30, duration, true, CardinalDirection.North);
                break;
            case CardinalDirection.South:
                Swing(330, 210, duration, false, CardinalDirection.South);
                break;
            case CardinalDirection.West:
                Swing(120, 240, duration, false, CardinalDirection.West);
                break;
            case CardinalDirection.East:
                Swing(60, 300, duration, true, CardinalDirection.East);
                break;
        }
    }

    private void Swing(int startAngle, int endAngle, float duration, bool clockwise, CardinalDirection swingDirection, int swingSpellId = -1)
    {
        // TODO: Melee Collider Data set up here
        if (clockwise && endAngle > startAngle) startAngle += 360;
        else if (!clockwise && startAngle > endAngle) endAngle += 360;

        Quaternion startRotation = Quaternion.Euler(0, 0, startAngle);
        Quaternion endRotation = Quaternion.Euler(0, 0, endAngle);
        
        MeleeCollider.SwingData swingData = new()
        {
              Damage = _toolItemSO.Damage,
              Knockback = _toolItemSO.Knockback,
              DetectionBetweenHitsDuration = _toolItemSO.DetectionBetweenHitsDuration,
              HitSound = _toolItemSO.HitSound,
              ColliderLength = _toolItemSO.ColliderLength
        };

        _ctx.PlayerRef.PlayerHand.MeleeCollider.StartSwing(swingData);
        _ctx.PlayerRef.PlayerHand.SwingDirection.Value = swingDirection;
        _ctx.PlayerRef.PlayerHand.PerformSwingClientRpc(startRotation, endRotation, duration, swingDirection);
    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        if (!_ctx.PlayerRef.PlayerHand.IsSwinging)
        {
            SwitchState(new AIStateData(AIState.Grounded, 0));
        }
    }

    public override void ExitState()
    {
        _ctx.SwingCooldownTimer.AddTime(_swingCd);
        if(_ctx.ServerCharacter.MovementState.Value == MovementState.Idle)
        {
            _ctx.ServerCharacter.CardinalDirection.Value = _swingDirection;
        }
    }
}
