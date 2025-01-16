using System;
using Pathfinding;
using UnityEngine;

public class PixieStateMachine : StateMachine<PixieStateMachine.PixieState>
{
	public enum PixieState
	{
		Attacking
	}
	
	[Range(1f, 100f)]
	[Tooltip("(Linear drag), higher this value, the more drag (knockback resistant) this entity experiences")]
	[SerializeField] private int _knockbackResist = 20; 
	[field: SerializeField] public float MoveForce { get; private set; }
	[Tooltip("Bias toward moving toward the player. Higher values mean stronger bias.")]
	[field: SerializeField] public float TowardPlayerBias { get; private set; }
	[field: SerializeField] public NpcWallCollider WallCollider { get; private set; }
	
	public Knockback KnockBack { get; private set; }
	private Npc _npc;
	public GridGraph GridGraph { get; private set; }
	public Rigidbody2D RB { get; private set; }
	
	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			// Get knockback component for dealing damage and knockback
			KnockBack = GetComponent<Knockback>();
			KnockBack.OnKnockbackStart += Knockback_OnKnockbackStart;
			KnockBack.OnKnockbackEnd += Knockback_OnKnockbackEnd;
		
			// Rigidbody setup
			RB = GetComponent<Rigidbody2D>();
			RB.linearDamping = _knockbackResist;
		
			// Set up Npc on death
			_npc = GetComponent<Npc>();
			_npc.OnNpcKilled += Npc_OnNpcKilled;
		
			// Set up the environment of the NPC so it knows how to pathfind itself
			GridGraph = NodeGraphUtility.GetGridGraph(GetComponent<NpcNetworkComponent>().GetNpcEnvironment());
			
			// Populate dictionary with livestock states
			_states[PixieState.Attacking] = new PixieAttackState(PixieState.Attacking, this);
			_currentState = _states[PixieState.Attacking];
		}
	
		base.OnNetworkSpawn();
	}
	
	protected override void Start()
	{
		if(!IsServer) return;
	
		base.Start();
	}

	private void Npc_OnNpcKilled(object sender, EventArgs e)
	{
		
	}

	private void Knockback_OnKnockbackStart(object sender, Knockback.KnockbackEventArgs e)
	{
		
	}

	private void Knockback_OnKnockbackEnd(object sender, Knockback.KnockbackEventArgs e)
	{
		
	}

	public override void OnDestroy()
	{
		if(IsServer)
		{
			_npc.OnNpcKilled -= Npc_OnNpcKilled;
			
			KnockBack.OnKnockbackStart -= Knockback_OnKnockbackStart;
			KnockBack.OnKnockbackEnd -= Knockback_OnKnockbackEnd;
		}
	}
}
