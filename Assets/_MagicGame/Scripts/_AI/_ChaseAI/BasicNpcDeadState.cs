using System;
using UnityEngine;


public class BasicNpcDeadState : BaseState
{
    private BasicNpcStateMachine _ctx;
    private Timer _despawnTimer;
    private Timer _itemDropTimer;
    private float _durationBeforeDespawn = 3f;
    private float _durationBeforeItemDrop = 0.3375f; // This should be the same as the death animation duration MMF_Player

    public BasicNpcDeadState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
        IsSuperState = true;
    }

    protected override void EnterState(AIStateData stateData)
    {
        _despawnTimer = new Timer(_durationBeforeDespawn);
        _despawnTimer.OnTimerEnd += OnDespawnTimerEnd;
        
        _itemDropTimer = new Timer(_durationBeforeItemDrop);
        _itemDropTimer.OnTimerEnd += OnItemDropTimerEnd;

        if (_ctx.ServerCharacter.TryGetComponent(out Collider2D collider2D))
        {
            collider2D.enabled = false;
        }
    }

    private void OnItemDropTimerEnd(object sender, EventArgs e)
    {
        _itemDropTimer.OnTimerEnd -= OnItemDropTimerEnd;

        if (_ctx.ServerCharacter.Data.IsNpc)
        {
            // Npc Death functionality here
            LootTable.SpawnLoot(_ctx.ServerCharacter.Data.LootTable, _ctx.ServerCharacter.transform.position, _ctx.ServerCharacter.CurrentBiome);
        }
    }

    private void OnDespawnTimerEnd(object sender, EventArgs e)
    {
        _despawnTimer.OnTimerEnd -= OnDespawnTimerEnd;
        
        // Despawn the NPC after the timer ends
        if (_ctx.ServerCharacter.NetworkObject.IsSpawned)
        {
            if (_ctx.ServerCharacter.TryGetComponent(out NpcNetworkVisibility npcVisibility))
            {
                npcVisibility.RemoveNpcServerRpc(_ctx.ServerCharacter.Data.SlotAmount);
            }
            
            _ctx.ServerCharacter.NetworkObject.Despawn();
        }
    }

    public override void UpdateState()
    {
        _despawnTimer?.Tick(Time.deltaTime);
        _itemDropTimer?.Tick(Time.deltaTime);
    }

    public override void CheckSwitchStates()
    {

    }

    public override void ExitState()
    {
        
    }

    public override void ClientEnterState(AIStateData stateData)
    {
        // Debug.Log($"Client: Entering Dead State for NPC {_ctx.ServerCharacter.gameObject.name}");
        // _ctx.ServerCharacter.ClientCharacter.Visuals.SetActive(false);
        _ctx.ServerCharacter.ClientFeedbacks.PlayDeathFeedbacksRpc();
    }
}