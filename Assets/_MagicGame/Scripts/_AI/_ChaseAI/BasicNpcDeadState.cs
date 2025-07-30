using UnityEngine;


public class BasicNpcDeadState : BaseState
{
    private BasicNpcStateMachine _ctx;

    public BasicNpcDeadState(AIState key, StateMachine context) : base(key, context)
    {
        _ctx = Context as BasicNpcStateMachine;
        IsSuperState = true;
    }

    protected override void EnterState(AIStateData stateData)
    {
        Debug.Log("NPC is dead. Cleaning up...");
        if (_ctx.ServerCharacter.Data.IsNpc)
        {
            // Npc Death functionality here
            LootTable.SpawnLoot(_ctx.ServerCharacter.Data.LootTable, _ctx.ServerCharacter.transform.position, _ctx.ServerCharacter.CurrentBiome);

            // Dispose NPC, need to figure out the correct order to safely dispose NPC and play death game feel
            _ctx.ServerCharacter.NetworkObject.Despawn();

            // NTFS: Need to figure out if this can be deleted, ALSO need to figure out how to get the Npc manager to work with this
            if (_ctx.ServerCharacter.TryGetComponent(out NpcNetworkVisibility npcVisibility))
            {
                npcVisibility.KillNpcServerRpc();
            }
        }
    }

    public override void ExitState()
    {
        // Cleanup if necessary
    }

    public override void UpdateState()
    {
        // No updates needed in dead state
    }

    public override void CheckSwitchStates()
    {
        
    }
}