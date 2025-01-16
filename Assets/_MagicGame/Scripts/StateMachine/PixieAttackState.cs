using Unity.Netcode;
using UnityEngine;

public class PixieAttackState : BaseState<PixieStateMachine.PixieState>
{
	private readonly PixieStateMachine _ctx;
	private Player _closestPlayer;

	private bool _isSwooping; // Tracks if the pixie is in its swooping phase
	private Vector3 _swoopDirection; // Direction during swooping
	private float _swoopTimer; // Timer for the swooping phase
	private float _cooldownTimer; // Timer for the cooldown after swooping

	private bool _isClockwise; // Flag to track the clockwise/counterclockwise direction

	public PixieAttackState(PixieStateMachine.PixieState key, StateMachine<PixieStateMachine.PixieState> context) : base(key, context)
	{
		_ctx = Context as PixieStateMachine;
	}

	public override void EnterState()
	{
		Debug.Log("Entering Attack State");
		_closestPlayer = GetClosestPlayer();
		Debug.Log($"Closest player is {_closestPlayer?.gameObject.name ?? "None"}");
		_isSwooping = false;
		_swoopTimer = 0f;
		_cooldownTimer = 0f;
		_isClockwise = Random.Range(0f, 1f) > 0.5f; // Randomly determine if it's clockwise or counterclockwise
	}

	public override void ExitState()
	{
	}

	public override void FixedUpdate()
	{
		if (_closestPlayer == null || !_closestPlayer.gameObject.activeInHierarchy)
		{
			// Re-acquire the closest player if the current one is no longer valid
			_closestPlayer = GetClosestPlayer();
			if (_closestPlayer == null) return; // No players available
		}

		// Handle cooldown
		if (_cooldownTimer > 0f)
		{
			_cooldownTimer -= Time.fixedDeltaTime;
		}

		if (_isSwooping)
		{
			HandleSwooping();
			return;
		}

		// Calculate direction to player
		Vector3 directionToPlayer = (_closestPlayer.transform.position + (Vector3)_closestPlayer.GetComponent<Collider2D>().offset - _ctx.transform.position).normalized;

		// Check proximity for starting a swoop
		float distanceToPlayer = Vector3.Distance(_closestPlayer.transform.position, _ctx.transform.position);
		if (distanceToPlayer < 3f && _cooldownTimer <= 0) // Swoop threshold distance
		{
			StartSwooping(directionToPlayer);
			return;
		}

		// Regular biased movement around the player
		Vector3 perpendicularDirection = Vector3.Cross(directionToPlayer, Vector3.forward).normalized; // Clockwise perpendicular

		// Switch direction randomly after the swoop phase ends
		if (!_isClockwise)
		{
			perpendicularDirection = -perpendicularDirection; // Counterclockwise
		}

		float biasRatio = _ctx.TowardPlayerBias;

		// Inverse bias if in cooldown
		if (_cooldownTimer > 0f)
		{
			biasRatio = 1f - _ctx.TowardPlayerBias;
		}

		Vector3 biasedDirection = directionToPlayer * biasRatio + perpendicularDirection * (1f - biasRatio);
		biasedDirection.Normalize(); // Ensure it's a unit vector

		// Calculate the dot product between the pixie's velocity and direction to the player
		float dotProduct = Vector3.Dot(_ctx.RB.linearVelocity.normalized, directionToPlayer);

		// The closer the dot product is to 1, the more aligned the Pixie is with the player
		// The closer it is to 0 (perpendicular), the slower the Pixie will go
		float speedFactor = Mathf.Abs(dotProduct); // Use the absolute value of the dot product to ensure positive scaling

		// Scale the speed to avoid reducing it below 60% of the normal speed
		float clampedSpeedFactor = Mathf.Max(speedFactor, 0.4f); // Clamp to 30% minimum speed

		// Apply biased force with speed adjustment
		_ctx.RB.AddForce(biasedDirection * _ctx.MoveForce * clampedSpeedFactor, ForceMode2D.Force);
	}

	public override PixieStateMachine.PixieState GetNextState()
	{
		return StateKey;
	}

	private void StartSwooping(Vector3 directionToPlayer)
	{
		_isSwooping = true;
		_swoopDirection = directionToPlayer; // Lock in the current direction
		_swoopTimer = 0.75f; // Duration of the swoop
	}

	private void HandleSwooping()
	{
		_swoopTimer -= Time.fixedDeltaTime;

		if (_swoopTimer <= 0f)
		{
			// End swooping phase and start cooldown
			_isSwooping = false;
			_cooldownTimer = 3f; // Cooldown duration after swoop

			// Randomly choose between clockwise or counterclockwise for the next swoop
			_isClockwise = Random.Range(0f, 1f) > 0.5f; 

			return;
		}

		// Continue moving in the swoop direction
		_ctx.RB.AddForce(_swoopDirection * _ctx.MoveForce * 2f, ForceMode2D.Force);
	}

	private Player GetClosestPlayer()
	{
		Player closestPlayer = null;
		float closestDistance = float.MaxValue;
		Vector3 pixiePosition = _ctx.transform.position; // Assuming the PixieStateMachine is attached to the pixie GameObject.

		foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
		{
			if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
			{
				// Check if the client has a Player object
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
}