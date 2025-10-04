using System;
using UnityEngine;



namespace ProjectWizard
{
	public class BasicNpcDeadState : BaseState
	{
	    private BasicNpcStateMachine _ctx;
	    private Timer _despawnTimer;
	    private float _durationBeforeDespawn = 4f;

	    public BasicNpcDeadState(AIState key, StateMachine context) : base(key, context)
	    {
	        _ctx = Context as BasicNpcStateMachine;
	        IsSuperState = true;
	    }

	    protected override void EnterState(AIStateData stateData)
	    {
	        _despawnTimer = new Timer(_durationBeforeDespawn);
	        _despawnTimer.OnTimerEnd += OnDespawnTimerEnd;

	        if (_ctx.ServerCharacter.Data.IsNpc)
	        {
	            // Npc Death functionality here
	            LootTable.SpawnLoot(_ctx.ServerCharacter.Data.LootTable, _ctx.ServerCharacter.transform.position, _ctx.ServerCharacter.CurrentBiome);
	        }

	        if (_ctx.ServerCharacter.TryGetComponent(out Collider2D collider2D))
	        {
	            collider2D.enabled = false;
	        }
        
	        _ctx.ServerCharacter.ClientCharacter.ColliderHolder.gameObject.SetActive(false);   
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
	    }

	    public override void CheckSwitchStates()
	    {

	    }

	    public override void ExitState()
	    {
        
	    }

	    public override void ClientEnterState(AIStateData stateData)
	    {
	        _ctx.ServerCharacter.ClientFeedbacks.PlayDeathFeedbacksRpc(stateData.Payload);
	    }
	}
}