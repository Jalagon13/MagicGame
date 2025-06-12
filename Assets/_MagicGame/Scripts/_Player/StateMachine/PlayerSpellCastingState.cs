using UnityEngine;

public class PlayerSpellCastingState : BaseState
{
    private PlayerStateMachine _ctx;
    private GameObject _clientChargeVfx;
    private SpellItemSO _spellToCast;
    private bool _performSpellStateCleanup;

    public PlayerSpellCastingState(AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log($"Player entering spell casting");
        _performSpellStateCleanup = true;
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
        if(!_ctx.SpellCaster.IsCasting)
        {
            SwitchState(new AIStateData(AIState.Grounded));
        }
        else if(_ctx.ServerCharacter.LifeState == LifeState.Dead)
        {
            _performSpellStateCleanup = false;
            SwitchState(new AIStateData(AIState.Dead));
        }
    }

    public override void ExitState()
    {
        if(_performSpellStateCleanup)
        {
            _ctx.ServerCharacter.Movement.StartKnockback(ActionManager.MouseWorldPosition, _spellToCast.Recoil);
        }

        _ctx.ServerCharacter.Stats.RemoveBuffsFromSource(_spellToCast);
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