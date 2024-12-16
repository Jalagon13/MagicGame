using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using Sirenix.OdinInspector;
using UnityEngine;

// Passively wander around and idle. Will flee when hit
[RequireComponent(typeof(AIPath))]
public class LivestockStateMachine : StateMachine<LivestockStateMachine.LivestockState>
{
	public enum LivestockState
	{
		Idle,
		Wandering,
		Fleeing
	}
	
	[Title("Livestock AI", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[MinMaxSlider(0, 10, true)]
	[SerializeField] private Vector2Int _idleTimeRange;
	[SerializeField] private int _wanderSpeed;
	[SerializeField] private int _fleeSpeed;
	[SerializeField] private int _maxWanderNodeDistance;
	[Range(1f, 100f)]
	[Tooltip("(Linear drag), higher this value, the more drag (knockback resistant) this entity experiences")]
	[SerializeField] private int _knockbackResist = 20; 
	[SerializeField] private List<SpriteAnimationHandler> _spriteDirectionHandlers = new();
	[SerializeField] private Collider2D _wallDetectionCollider; // Used for wall collision detection
	
	private Npc _npc;
	private Knockback _knockback;
	private Rigidbody2D _rb;
	private CardinalDirection _previousDirection;
	private CardinalDirection _currentDirection;

	public Vector3 ThreatSource { get; private set; }
	public AIPath Agent { get; private set; }
	public CardinalDirection LookDirection { get; private set; }
	public int MinIdleTime => _idleTimeRange.x;
	public int MaxIdleTime => _idleTimeRange.y;
	public int MaxWanderNodeDistance => _maxWanderNodeDistance;
	public int FleeSpeed => _fleeSpeed;
	public int WanderSpeed => _wanderSpeed;
	public bool IsMoving { get; set; }

	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			InitializeServerComponents();
		}
	
		base.OnNetworkSpawn();
	}
	
	private void InitializeServerComponents()
	{
		// Populate dictionary with livestock states
		_states[LivestockState.Idle] = new LivestockIdleState(LivestockState.Idle, this);
		_states[LivestockState.Wandering] = new LivestockWanderingState(LivestockState.Wandering, this);
		_states[LivestockState.Fleeing] = new LivestockFleeingState(LivestockState.Fleeing, this);
		_currentState = _states[LivestockState.Idle];
		
		// Get knockback component for dealing damage and knockback
		_knockback = GetComponent<Knockback>();
		_knockback.OnKnockbackStart += Knockback_OnKnockbackStart;
		_knockback.OnKnockbackEnd += Knockback_OnKnockbackEnd;
		
		// Rigidbody setup
		_rb = GetComponent<Rigidbody2D>();
		_rb.linearDamping = _knockbackResist;
		
		// Get AIPath component for movement
		Agent = GetComponent<AIPath>();
		Agent.maxSpeed = _wanderSpeed;
		
		// Set up Npc on death
		_npc = GetComponent<Npc>();
		_npc.OnNpcKilled += Npc_OnNpcKilled;
	}

	protected override void Start()
	{
		if(!IsServer) return;
	
		base.Start();
	}

	protected override void FixedUpdate()
	{
		if(!IsServer) return;
	
		base.FixedUpdate();
	
		if(Agent.desiredVelocity.magnitude > 0.1f)
		{
			// Get the current velocity and convert it to a cardinal direction.
			_currentDirection = GetCardinalDirection(Agent.desiredVelocity);

			// Check if the direction has changed.
			if (_currentDirection != _previousDirection)
			{
				// Call a method to handle the direction change.
				OnDirectionChange(_currentDirection);

				// Update the previous direction to the current one.
				_previousDirection = _currentDirection;
			}
		} 
	}
	
	private void Npc_OnNpcKilled(object sender, EventArgs e)
	{
		TransitionToState(LivestockState.Idle);
		Agent.canMove = false;
		Agent.canSearch = false;
		if(Agent.hasPath)
		{
			Agent.SetPath(null);
		}
	}
	
	public bool DestinationValid(Vector3 moveSpot)
	{
		var tilePos = new Vector3Int((int)moveSpot.x, (int)moveSpot.y);
		return Environment.Instance.GetGroundTilemapData().GetTilemap().HasTile(tilePos);
	}
	
	private void Knockback_OnKnockbackStart(object sender, Knockback.KnockbackEventArgs e)
	{
		_wallDetectionCollider.isTrigger = false;
		
		Agent.enabled = false;
	}
	
	private void Knockback_OnKnockbackEnd(object sender, Knockback.KnockbackEventArgs e)
	{
		_wallDetectionCollider.isTrigger = true;
		
		Agent.enabled = true;
		
		ThreatSource = e.KnockBackerPosition;
		
		TransitionToState(LivestockState.Fleeing);
	}
	
	// This method will be called when the movement direction changes.
	public void OnDirectionChange(CardinalDirection newDirection)
	{
		// Add your code here to handle the direction change, such as playing animations or sounds.
		LookDirection = newDirection;
		
		foreach(var handler in _spriteDirectionHandlers)
		{
			if(IsMoving)
			{
				handler.PlayMoveAnimation(LookDirection);
			}
			else
			{
				handler.PlayIdleAnimation(LookDirection);
			}
		}
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
		if(IsServer)
		{
			_npc.OnNpcKilled -= Npc_OnNpcKilled;
			
			_knockback.OnKnockbackStart -= Knockback_OnKnockbackStart;
			_knockback.OnKnockbackEnd -= Knockback_OnKnockbackEnd;
		}
	}
}
