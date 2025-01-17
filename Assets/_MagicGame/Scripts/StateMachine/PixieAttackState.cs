using Unity.Netcode;
using UnityEngine;

public class PixieAttackState : BaseState<PixieStateMachine.PixieState>
{
	private readonly PixieStateMachine _ctx;
	private Player _closestPlayer;
	private Vector3 _wallAvoidanceDirection; // Direction to avoid walls
	private bool _isAvoidingWall = false; // Flag for wall avoidance
	private bool _flipPerpendicularDirection = false; // Flag to flip perpendicular direction

	// New variables for knockback transition
	private float _knockbackTransitionTime = 1.5f; // Duration of transition
	private float _knockbackTimer = 0f; // Timer to track transition time
	private float _originalTowardPlayerBias; // Store the original TowardPlayerBias
	private float _biasModifier = 0f; // New modifier for TowardPlayerBias

	public PixieAttackState(PixieStateMachine.PixieState key, StateMachine<PixieStateMachine.PixieState> context) : base(key, context)
	{
		_ctx = Context as PixieStateMachine;
		_ctx.KnockBack.OnKnockbackEnd += OnKnockbackEnd;
		_ctx.WallCollider.OnWallCollide += OnWallCollide;
	}

	private void OnKnockbackEnd(object sender, Knockback.KnockbackEventArgs e)
	{
		// Store the original TowardPlayerBias value when knockback ends
		_originalTowardPlayerBias = _ctx.TowardPlayerBias;

		// Reset timer for the transition
		_knockbackTimer = 0f;

		// Initialize the bias modifier to 0 (start at 0)
		_biasModifier = 0f;

		if(Random.value > 0.5)
		{
			FlipPerpendicularDirection();
		}
	}

	private void OnWallCollide(object sender, NpcWallCollider.WallCollisionEventArgs e)
	{
		// Calculate avoidance direction based on contact point
		Vector3 contactPoint = e.ContactPoint;
		Vector3 pixiePosition = _ctx.transform.position;
		Vector3 collisionDirection = (pixiePosition - contactPoint).normalized; // Direction away from collision point

		// Store the avoidance direction and activate avoidance
		_wallAvoidanceDirection = collisionDirection;
		_isAvoidingWall = true;
		
		// Flip perpendicular direction
		FlipPerpendicularDirection();
	}

	public override void EnterState()
	{
		Debug.Log("Entering Attack State");

		// Find the closest player
		_closestPlayer = GetClosestPlayer();
		Debug.Log($"Closest player is {_closestPlayer?.gameObject.name}");
	}

	public override void ExitState()
	{
		_ctx.KnockBack.OnKnockbackEnd -= OnKnockbackEnd;
		_ctx.WallCollider.OnWallCollide -= OnWallCollide;
	}

	public override void FixedUpdate()
	{
		// Handle the knockback transition
		if (_knockbackTimer < _knockbackTransitionTime)
		{
			_knockbackTimer += Time.fixedDeltaTime; // Update the timer
			// Lerp the bias modifier from 0 to 1 over the transition time
			_biasModifier = Mathf.Lerp(0f, 1f, _knockbackTimer / _knockbackTransitionTime);
		}

		// Apply the bias modifier to TowardPlayerBias
		float effectiveTowardPlayerBias = _ctx.TowardPlayerBias * _biasModifier;

		// Re-acquire the closest player each frame
		_closestPlayer = GetClosestPlayer();
		if (_closestPlayer == null || !_closestPlayer.gameObject.activeInHierarchy)
		{
			return; // No valid players available
		}

		// Get directions
		Vector3 directionToPlayer = (_closestPlayer.transform.position - _ctx.transform.position).normalized;
		Vector3 currentVelocityDirection = _ctx.RB.linearVelocity.normalized;

		if (_ctx.RB.linearVelocity.magnitude < 0.1f)
		{
			currentVelocityDirection = Vector3.zero; // Prevent jittery behavior at low speeds
		}

		// Calculate perpendicular direction to the player
		Vector3 perpendicularDirection = Vector3.Cross(directionToPlayer, Vector3.forward).normalized;

		// Flip perpendicular direction if the flag is set
		if (_flipPerpendicularDirection)
		{
			perpendicularDirection = -perpendicularDirection;
		}

		// Determine the movement direction, prioritizing wall avoidance if active
		Vector3 movementDirection = _isAvoidingWall
			? (_wallAvoidanceDirection + directionToPlayer * effectiveTowardPlayerBias).normalized
			: (directionToPlayer * effectiveTowardPlayerBias +
			   perpendicularDirection * (1f - effectiveTowardPlayerBias) +
			   currentVelocityDirection * 0.85f).normalized;

		// Reset wall avoidance after using it for one frame
		_isAvoidingWall = false;

		// Calculate angle between current velocity and the desired movement direction
		float angle = Vector3.Angle(currentVelocityDirection, directionToPlayer);

		// Scale speed based on the angle (up to 50% slower for sharp turns)
		float speedModifier = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(angle / 90f));

		// Apply force to Rigidbody
		_ctx.RB.AddForce(movementDirection * _ctx.MoveForce * speedModifier, ForceMode2D.Force);
	}

	public override PixieStateMachine.PixieState GetNextState()
	{
		return StateKey; // Remain in the current state
	}

	private Player GetClosestPlayer()
	{
		Player closestPlayer = null;
		float closestDistance = float.MaxValue;
		Vector3 pixiePosition = _ctx.transform.position;

		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
			{
				var player = client.PlayerObject?.GetComponent<Player>();
				if (player != null)
				{
					float distance = Vector3.Distance(pixiePosition, player.transform.position);
					if (distance < closestDistance)
					{
						closestDistance = distance;
						closestPlayer = player;
					}
				}
			}
		}

		return closestPlayer;
	}

	private void FlipPerpendicularDirection()
	{
		_flipPerpendicularDirection = !_flipPerpendicularDirection;
	}
}