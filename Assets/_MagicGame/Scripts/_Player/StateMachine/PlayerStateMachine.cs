using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using FMOD.Studio;
using UnityEditor.ShaderGraph.Internal;

public enum CardinalDirection
{
	North,
	South,
	West,
	East
}

public class PlayerStateMachine : StateMachine
{
	public PlayerStateMachine(ServerCharacter serverCharacter)
	{
		_serverCharacter = serverCharacter;

		_states[AIState.Idle] = new PlayerIdleState(AIState.Idle, this);
		_states[AIState.Moving] = new PlayerMoveState(AIState.Moving, this);
		_states[AIState.Grounded] = new PlayerGroundedState(AIState.Grounded, this);
		_states[AIState.Attacking] = new PlayerAttackState(AIState.Attacking, this);
		_states[AIState.Knockbacked] = new PlayerKnockbackedState(AIState.Knockbacked, this);
		_states[AIState.SpellCasting] = new PlayerSpellCastingState(AIState.SpellCasting, this);
		_currentState = _states[AIState.Grounded];
	}

    public override void OwnerInitialization()
    {
		WorldManager.Instance.OnBiomeTransitionStart += WorldManager_RestrictMovement;
		WorldManager.Instance.OnBiomeTransitionEnd += WorldManager_AllowMovement;
	}
	
	public override void Dispose()
	{
		WorldManager.Instance.OnBiomeTransitionStart -= WorldManager_RestrictMovement;
		WorldManager.Instance.OnBiomeTransitionEnd -= WorldManager_AllowMovement;
	}

	public override void ReceiveHP(ServerCharacter inflicter, int amount)
	{
		if (inflicter != null)
		{
			if (amount < 0)
			{
				// Damaged
			}
			else
			{
				// Healed
			}
		}
	}

	private void WorldManager_AllowMovement(object sender, EventArgs e)
	{
		// CanMove = true;
	}

	private void WorldManager_RestrictMovement(object sender, EventArgs e)
	{
		// CanMove = false;
	}

	// This method returns a cardinal direction based on the velocity.
	private CardinalDirection GetCardinalDirection(Vector3 velocity)
	{
		if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
		{
			return (velocity.x > 0) ? CardinalDirection.East : CardinalDirection.West;
		}
		else
		{
			return (velocity.y > 0) ? CardinalDirection.North : CardinalDirection.South;
		}
	}
}
