using UnityEngine;

public class PlayerSpellCastingState : BaseState
{
    private PlayerStateMachine _ctx;
    private GameObject _clientChargeVfx;
    private SpellItemSO _spellToCast;

    public PlayerSpellCastingState(AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        _spellToCast = GameManager.Instance.GetItemSOFromItemId(stateData.Amount) as SpellItemSO;
        
        Buff castingMoveBuff = new Buff(
            _ctx.ServerCharacter.Stats.MovementSpeed, 
            new StatModifier(_spellToCast.HasteMultiplier, StatModifierType.Percent, _spellToCast)/* ,
            _spellToCast.CastTime */);
        
        _ctx.ServerCharacter.Stats.AddBuff(castingMoveBuff);
    }

    public override void UpdateState()
    {
        
    }

    public override void CheckSwitchStates()
    {
        if(!SpellManager.Instance.IsCasting)
        {
            SwitchState(new AIStateData(AIState.Grounded));
        }
    }

    public override void ExitState()
    {
        _ctx.ServerCharacter.Stats.RemoveBuffsFromSource(_spellToCast);
        _ctx.ServerCharacter.Movement.StartKnockback(ActionManager.MouseWorldPosition, _spellToCast.Recoil);
    }

    public override void ClientEnterState(AIStateData stateData)
    {
        SpellItemSO spellToCast = GameManager.Instance.GetItemSOFromItemId(stateData.Amount) as SpellItemSO;

        _clientChargeVfx = Object.Instantiate(spellToCast.ChargeVFX, _ctx.PlayerRef.PlayerHand.SpellSpawnTransform);
        _clientChargeVfx.transform.localPosition = Vector3.zero;
        _clientChargeVfx.GetComponent<MagicCircle>().StartAnimation(spellToCast.CastTime);
    }

    public override void ClientUpdateState(AIStateData stateData)
    {
        
    }
    
    public override void ClientExitState(AIStateData stateData)
    {
        _clientChargeVfx?.GetComponent<MagicCircle>().StopAnimation();
    }
}