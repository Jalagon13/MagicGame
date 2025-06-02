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

public class PlayerStateMachine : StateMachine<AIState>
{
	private ServerCharacter _serverCharacter;
	public ServerCharacter ServerCharacter => _serverCharacter;
	public CharacterDataSO CharacterData => _serverCharacter.Data;

	public PlayerStateMachine(ServerCharacter serverCharacter)
	{
		_serverCharacter = serverCharacter;

		_states[AIState.Idle] = new PlayerIdleState(AIState.Idle, this);
		_states[AIState.Moving] = new PlayerMoveState(AIState.Moving, this);
		_states[AIState.Grounded] = new PlayerGroundedState(AIState.Grounded, this);
		_currentState = _states[AIState.Grounded];
		EnterCurrentState();
		
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
