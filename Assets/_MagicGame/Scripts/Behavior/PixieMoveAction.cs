using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using Unity.Netcode;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PixieMove", story: "[Self] moves to closest player with [moveDirectionBias] and [moveForce]", category: "Action", id: "a63d717cef313c1b7800612a3a2c3377")]
public partial class PixieMoveAction : Action
{
	[SerializeReference] public BlackboardVariable<GameObject> Self;
	[SerializeReference] public BlackboardVariable<float> MoveDirectionBias;
	[SerializeReference] public BlackboardVariable<float> MoveForce;
	[SerializeReference] public BlackboardVariable<WallDetectorCollider> WallDetectorCollider;
	
	private Rigidbody2D _rb2d;
	private Player _closestPlayer;
	private float _biasModifier;
	private float _knockbackTimer = 0f; // Timer to track transition time
	private bool _flipPerpendicularDirection = false; // Flag to flip perpendicular direction
	private float _knockbackTransitionTime = 1.5f; // Duration of transition
	private float fixedUpdateInterval = 0.02f; // Default FixedUpdate interval (50 FPS)
	private float fixedUpdateTimer = 0f;

	protected override Status OnStart()
	{
		_rb2d = Self.Value.GetComponent<Rigidbody2D>();
		Self.Value.GetComponent<Knockback>().OnKnockbackEnd += OnKnockbackEnd; 
		WallDetectorCollider.Value.OnWallCollide += OnWallCollide;
	
		return Status.Running;
	}
	
	private void OnWallCollide(object sender, WallDetectorCollider.WallCollisionEventArgs e)
	{
		// Flip perpendicular direction
		if(UnityEngine.Random.value > 0.5)
		{
			FlipPerpendicularDirection();
		}
	}

	private void OnKnockbackEnd(object sender, Knockback.KnockbackEventArgs e)
	{
		// Reset timer for the transition
		_knockbackTimer = 0f;

		// Initialize the bias modifier to 0 (start at 0)
		_biasModifier = 0f;

		if(UnityEngine.Random.value > 0.5)
		{
			FlipPerpendicularDirection();
		}
	}
	
	private void FlipPerpendicularDirection()
	{
		_flipPerpendicularDirection = !_flipPerpendicularDirection;
	}

	protected override Status OnUpdate()
	{
		// Accumulate time
		fixedUpdateTimer += Time.deltaTime;

		// Run logic only when enough time has passed
		if (fixedUpdateTimer >= fixedUpdateInterval)
		{
			fixedUpdateTimer -= fixedUpdateInterval; // Reset timer
			RunFixedUpdateLogic();
		}

		return Status.Running;
	}

	private void RunFixedUpdateLogic()
	{
		// Handle the knockback transition
		if (_knockbackTimer < _knockbackTransitionTime)
		{
			_knockbackTimer += Time.fixedDeltaTime; // Update the timer
			// Lerp the bias modifier from 0 to 1 over the transition time
			_biasModifier = Mathf.Lerp(0f, 1f, _knockbackTimer / _knockbackTransitionTime);
		}

		// Apply the bias modifier to TowardPlayerBias
		float effectiveTowardPlayerBias = MoveDirectionBias * _biasModifier;

		// Re-acquire the closest player each frame
		_closestPlayer = GetClosestPlayer();
		if (_closestPlayer == null || !_closestPlayer.gameObject.activeInHierarchy)
		{
			return; // No valid players available
		}

		// Get directions
		Vector3 directionToPlayer = (_closestPlayer.transform.position - Self.Value.transform.position).normalized;
		Vector3 currentVelocityDirection = _rb2d.linearVelocity.normalized;

		if (_rb2d.linearVelocity.magnitude < 0.1f)
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
		Vector3 movementDirection = (directionToPlayer * effectiveTowardPlayerBias +
		   perpendicularDirection * (1f - effectiveTowardPlayerBias) +
		   currentVelocityDirection * 0.85f).normalized;

		// Calculate angle between current velocity and the desired movement direction
		float angle = Vector3.Angle(currentVelocityDirection, directionToPlayer);

		// Scale speed based on the angle (up to 50% slower for sharp turns)
		float speedModifier = Mathf.Lerp(1f, 0.5f, Mathf.Clamp01(angle / 90f));

		// Apply force to Rigidbody
		_rb2d.AddForce(movementDirection * MoveForce.Value * speedModifier, ForceMode2D.Force);
	}
	
	private Player GetClosestPlayer()
	{
		Player closestPlayer = null;
		float closestDistance = float.MaxValue;
		Vector3 pixiePosition = Self.Value.transform.position;

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

	protected override void OnEnd()
	{
	}
}

