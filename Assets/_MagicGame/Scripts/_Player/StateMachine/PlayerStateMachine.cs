using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using FMOD.Studio;
using UnityEditor.ShaderGraph.Internal;

public enum CardinalDirection
{
	None,
	North,
	South,
	West,
	East
}

public class PlayerStateMachine : StateMachine
{
	private ItemSO _heldItem;
	public ItemSO HeldItem => _heldItem;
	
	private Timer _swingCdTimer;
	public Timer SwingCooldownTimer => _swingCdTimer;
	
	private Player _playerRef;
	public Player PlayerRef => _playerRef;
	
	private SpellCaster _spellCaster;
	public SpellCaster SpellCaster => _spellCaster;
	
	public PlayerStateMachine(ServerCharacter serverCharacter)
	{
		// This constructor gets played on all client machines
		_serverCharacter = serverCharacter;
		_swingCdTimer = new(0f);

		_states[AIState.Idle] = new PlayerIdleState(AIState.Idle, this);
		_states[AIState.Moving] = new PlayerMoveState(AIState.Moving, this);
		_states[AIState.Knockbacked] = new PlayerKnockbackedState(AIState.Knockbacked, this);
		_states[AIState.Grounded] = new PlayerGroundedState(AIState.Grounded, this);
		_states[AIState.Attacking] = new PlayerAttackState(AIState.Attacking, this);
		_states[AIState.SpellCasting] = new PlayerSpellCastingState(AIState.SpellCasting, this);
		_states[AIState.Dead] = new PlayerDeadState(AIState.Dead, this);
		_currentState = _states[AIState.Grounded];
		
		if (_serverCharacter.TryGetComponent(out Player player))
		{
			_playerRef = player;
			_playerRef.SelectedItemIdNetworkVariable.OnValueChanged += OnSelectedItemIdChanged; 
		}
		
		if(_serverCharacter.TryGetComponent(out SpellCaster spellCaster))
		{
		    _spellCaster = spellCaster;
		}
	}

    public override void OwnerInitialization()
    {

	}
	
	public override void Dispose()
	{
		if (_serverCharacter.TryGetComponent(out Player player))
		{
			player.SelectedItemIdNetworkVariable.OnValueChanged -= OnSelectedItemIdChanged;
		}
	}

    public override void UpdateAI()
    {
        base.UpdateAI();

		_swingCdTimer?.Tick(Time.deltaTime);
	}

    private void OnSelectedItemIdChanged(int previousValue, int newValue)
    {
		// Played on all client machines for this player instance
		_heldItem = GameManager.Instance.GetItemSOFromItemId(newValue);
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
}
