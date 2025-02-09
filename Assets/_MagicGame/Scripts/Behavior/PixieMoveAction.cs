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
	[SerializeReference] public BlackboardVariable<float> Speed;
	[SerializeReference] public BlackboardVariable<float> TurnSharpness;
	[SerializeReference] public BlackboardVariable<WallDetectorCollider> WallDetectorCollider;
	
	private Rigidbody2D _rb2d;
	private Player _closestPlayer;
	private bool _clockwise = false; // Flag to flip perpendicular direction
	private float fixedUpdateInterval = 0.02f; // Default FixedUpdate interval (50 FPS)
	private float fixedUpdateTimer = 0f;
	private Knockback _knockback;
	private Vector2 _velocity;

	protected override Status OnStart()
	{
		WallDetectorCollider.Value.OnWallCollide += OnWallCollide;
		
		_rb2d = Self.Value.GetComponent<Rigidbody2D>();
		_knockback = Self.Value.GetComponent<Knockback>();
		_knockback.OnKnockbackStart += OnKnockbackStart; 
	
		return Status.Running;
	}
	
	private void OnWallCollide(object sender, WallDetectorCollider.WallCollisionEventArgs e)
	{
		_knockback.ApplyKnockback(e.ContactPoint, 0, UnityEngine.Random.Range(7, 10));
		if(UnityEngine.Random.value > 0.5)
		{
			_clockwise = !_clockwise;
		}
	}

	private void OnKnockbackStart(object sender, Knockback.KnockbackEventArgs e)
	{
		if(UnityEngine.Random.value > 0.5)
		{
			_clockwise = !_clockwise;
		}
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
		// Re-acquire the closest player each frame
		_closestPlayer = GetClosestPlayer();
		if (_closestPlayer == null || !_closestPlayer.gameObject.activeInHierarchy)
		{
			return;
		}

		Vector2 directionToPlayer = (_closestPlayer.transform.position - Self.Value.transform.position).normalized;
		
		Vector2 perpendicularDirection = _clockwise // Whether perpendicular is clockwise or not
			? new Vector2(directionToPlayer.y, -directionToPlayer.x) 
			: new Vector2(-directionToPlayer.y, directionToPlayer.x);
			
		Vector2 desiredDirection = directionToPlayer + (perpendicularDirection * MoveDirectionBias);
		
		desiredDirection.Normalize();
		
		if(_knockback.Velocity.magnitude > 0)
		{
			_velocity = desiredDirection + _knockback.Velocity;
		}
		else
		{
			_velocity = Vector2.Lerp(_velocity, desiredDirection * Speed.Value, TurnSharpness.Value * Time.fixedDeltaTime);
		}
		
		_rb2d.MovePosition(_rb2d.position + _velocity * Time.fixedDeltaTime);
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

