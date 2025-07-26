using UnityEngine;

public class PlayerSpellCastingState : BaseState
{
    private PlayerStateMachine _ctx;
    private GameObject _clientChargeVfx;
    private SpellCastGroup _currentSpellCastGroup;

    public PlayerSpellCastingState(AIState key, StateMachine context) : base(key, context)
    {
        IsSuperState = true;
        _ctx = Context as PlayerStateMachine;
    }

    protected override void EnterState(AIStateData stateData)
    {
        // Debug.Log($"Player entering spell casting");
        _currentSpellCastGroup = _ctx.SpellCaster.CurrentSpellGroup;

        float totalHasteMultiplier = 0;
        int count = _currentSpellCastGroup.SpellsToCast.Count;

        foreach (SpellMetaData spellMetaData in _currentSpellCastGroup.SpellsToCast)
        {
            totalHasteMultiplier += spellMetaData.SpellItem.HasteMultiplier;
        }

        float averageHasteMultiplier = count > 0 ? totalHasteMultiplier / count : 0;

        Buff castingMoveBuff = new(
            _ctx.ServerCharacter.Stats.MovementSpeed,
            new StatModifier(averageHasteMultiplier, StatModifierType.Percent, _currentSpellCastGroup)/* ,
            _spellToCast.CastTime */);
        
        _ctx.ServerCharacter.Stats.AddBuff(castingMoveBuff);
    }

    public override void UpdateState()
    {

    }

    public override void CheckSwitchStates()
    {
        if(!_ctx.SpellCaster.IsCasting.Value)
        {
            SwitchState(new AIStateData(AIState.Grounded));
        }
        else if(_ctx.ServerCharacter.LifeState == LifeState.Dead)
        {
            SwitchState(new AIStateData(AIState.Dead));
        }
    }

    public override void ExitState()
    {
        _ctx.ServerCharacter.Stats.RemoveBuffsFromSource(_currentSpellCastGroup);
    }

    public override void ClientEnterState(AIStateData stateData)
    {
        SpellItemSO spellToCast = GameManager.Instance.GetItemSOFromItemId(stateData.Amount) as SpellItemSO;

        _clientChargeVfx = Object.Instantiate(spellToCast.ChargeVFX, _ctx.SpellCaster.SpellSpawnTransform);
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