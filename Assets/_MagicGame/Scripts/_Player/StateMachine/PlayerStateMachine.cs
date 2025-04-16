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

public class PlayerStateMachine : StateMachine<PlayerStateMachine.PlayerState>
{
	public enum PlayerState
	{
		Idle,
		Moving
	}
	
	[SerializeField] private PlayerHand _mainHand;
	[SerializeField] private List<SpriteAnimationHandler> _spriteAnimationHandlerList = new();
	
	private NetworkVariable<Vector2> _moveVectorNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private CardinalDirection _previousDirection;
	private CardinalDirection _currentDirection;
	private bool _previousIsMoving; // Tracks the previous frame's IsMoving value
	private Player _thisPlayer;
	
	public PlayerStats PlayerStats { get; private set; }
	public Knockback Knockback { get; private set; }
	public Vector2 MoveVector { get { return _moveVectorNetworkVariable.Value; } set { if(IsOwner) {_moveVectorNetworkVariable.Value = value; } } }
	public Rigidbody2D RigidBody2D { get; private set; }
	public PlayerIdleState PlayerIdleState { get; private set; }
	public PlayerMoveState PlayerMoveState { get; private set; }
	public CardinalDirection FacingDirection { get {return _currentDirection; } set {_currentDirection = value; } }
	public bool IsMoving { get; set; }
	public bool IsDead { get { return _thisPlayer.HealthState.IsDead; } }
	public bool CanMove { get; private set; } = true;
	public Vector2 Velocity { get; set; }
	
	private void Awake()
	{
		_states[PlayerState.Idle] = new PlayerIdleState(PlayerState.Idle, this); 
		_states[PlayerState.Moving] = new PlayerMoveState(PlayerState.Moving, this); 
		_currentState = _states[PlayerState.Idle];
	
		PlayerIdleState = _states[PlayerState.Idle] as PlayerIdleState;
		PlayerMoveState = _states[PlayerState.Moving] as PlayerMoveState;
		
		RigidBody2D = GetComponent<Rigidbody2D>();
		Knockback = GetComponent<Knockback>();
		PlayerStats = GetComponent<PlayerStats>();

		_thisPlayer = GetComponent<Player>();
		_thisPlayer.OnDeath += Player_OnKilled;
		_thisPlayer.OnRespawn += Player_OnRespawn;
	}
	
	public override void OnNetworkSpawn()
	{
		_mainHand.OnSwingStart += OnSwingStart;
		_mainHand.OnSwingEnd += OnSwingEnd;
		_mainHand.OnCastingArmDirectionChanged += OnCastingArmDirectionChanged;
		_mainHand.OnHoldingWandStart += OnHoldingWandStart;
		_mainHand.OnHoldingWandEnd += OnHoldingWandEnd;
		
		if(IsOwner)
		{
			GameInput.Instance.OnMove += GameInput_OnMove;
			WorldManager.Instance.OnBiomeTransitionStart += WorldManager_RestrictMovement;
			WorldManager.Instance.OnBiomeTransitionEnd += WorldManager_AllowMovement;
		}
	
		base.OnNetworkSpawn();
	}

	protected override void FixedUpdate()
	{
		base.FixedUpdate();

		var isHoldingWand = _thisPlayer.IsHoldingAWand();
		var isSwingOnGoing = _thisPlayer.IsPerformingSwing;
		
		if(!isHoldingWand && !isSwingOnGoing)
		{
			UpdateDirectionBasedOnMoveVector();
		}
		
		// Check if IsMoving has changed this frame
		if (IsMoving != _previousIsMoving)
		{
			// Call a method or handle logic when the state changes
			PlayAnimationBasedOnDirection(_currentDirection);
		}

		// Update _previousIsMoving to the current IsMoving value
		_previousIsMoving = IsMoving;
	}
	
	private void WorldManager_AllowMovement(object sender, EventArgs e)
	{
		CanMove = true;
	}

	private void WorldManager_RestrictMovement(object sender, EventArgs e)
	{
		CanMove = false;
	}

	private void Player_OnKilled(object sender, EventArgs e)
	{
		// This code should only be run on the player instance on the machine whose player died
		if(OwnerClientId == NetworkManager.LocalClientId)
		{
			_moveVectorNetworkVariable.Value = Vector2.zero;
		}
	}
	
	private void Player_OnRespawn(object sender, EventArgs e)
	{
		PlayAnimationBasedOnDirection(_currentDirection);
	}
	
	private void UpdateDirectionBasedOnMoveVector()
	{
		if (_moveVectorNetworkVariable.Value.magnitude > 0.1f)
		{
			// Get the current velocity and convert it to a cardinal direction.
			_currentDirection = GetCardinalDirection(_moveVectorNetworkVariable.Value);

			// Check if the direction has changed.
			if (_currentDirection != _previousDirection)
			{
				// Call a method to handle the direction change.
				PlayAnimationBasedOnDirection(_currentDirection);

				// Update the previous direction to the current one.
				_previousDirection = _currentDirection;
			}
		} 
	}
	
	private void GameInput_OnMove(object sender, InputAction.CallbackContext context)
	{
		_moveVectorNetworkVariable.Value = context.ReadValue<Vector2>();
	}
	
	public void PlayAnimationBasedOnDirection(CardinalDirection newDirection)
	{
		// Add your code here to handle the direction change, such as playing animations or sounds.
		FacingDirection = newDirection;
		
		foreach(var handler in _spriteAnimationHandlerList)
		{
			if(MoveVector.magnitude == 0 && Velocity.magnitude < 0.75f)
			{
				handler.PlayIdleAnimation(FacingDirection);
			}
			else
			{
				handler.PlayMoveAnimation(FacingDirection);
			}
		}
	}
	
	private void OnHoldingWandStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		PlayAnimationBasedOnDirection(e.Direction);
	}
	
	private void OnHoldingWandEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		if(_thisPlayer.IsHoldingAWand())
		{
			PlayAnimationBasedOnDirection(e.Direction);
		}
		else
		{
			UpdateDirectionBasedOnMoveVector();
			PlayAnimationBasedOnDirection(IsMoving ? FacingDirection : e.Direction);
		}
	}
	
	private void OnCastingArmDirectionChanged(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		PlayAnimationBasedOnDirection(e.Direction);
	}
	
	private void OnSwingEnd(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		UpdateDirectionBasedOnMoveVector();
		PlayAnimationBasedOnDirection(IsMoving ? FacingDirection : e.Direction);
	}
	
	private void OnSwingStart(object sender, PlayerHand.CardinalDirectionEventArgs e)
	{
		PlayAnimationBasedOnDirection(e.Direction);
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
	
	public override void OnDestroy()
	{
		_mainHand.OnSwingStart -= OnSwingStart;
		_mainHand.OnSwingEnd -= OnSwingEnd;
		_mainHand.OnCastingArmDirectionChanged -= OnCastingArmDirectionChanged;
		_mainHand.OnHoldingWandStart -= OnHoldingWandStart;
		_mainHand.OnHoldingWandEnd -= OnHoldingWandEnd;
		
		_thisPlayer.OnDeath -= Player_OnKilled;
		_thisPlayer.OnRespawn -= Player_OnRespawn;
		
		if(IsOwner)
		{
			GameInput.Instance.OnMove -= GameInput_OnMove;
			WorldManager.Instance.OnBiomeTransitionStart -= WorldManager_RestrictMovement;
			WorldManager.Instance.OnBiomeTransitionEnd -= WorldManager_AllowMovement;
		}
	}
}
