using System;
using Unity.Netcode;
using UnityEngine;

public class PixieAI : MonoBehaviour
{
	[SerializeField] private float _speed;
	[SerializeField] private float _turnSharpness;
	[SerializeField] private float _moveDirectionBias;

	private Rigidbody2D _rb2d;
	private Player _closestPlayer;
	private Knockback _knockback;
	private Vector2 _velocity;
	private bool _clockwise = false;

	private void Awake()
	{
		_rb2d = GetComponent<Rigidbody2D>();
		_knockback = GetComponent<Knockback>();
		_knockback.OnKnockbackStart += OnKnockbackStart;
	}

	private void OnKnockbackStart(object sender, Knockback.KnockbackEventArgs e)
	{
		_clockwise = !_clockwise;
	}

	private void FixedUpdate()
	{
		_closestPlayer = GetClosestPlayer();
		if (_closestPlayer == null || !_closestPlayer.gameObject.activeInHierarchy)
		{
			return;
		}

		Vector2 directionToPlayer = (_closestPlayer.transform.position - transform.position).normalized;

		Vector2 perpendicularDirection = _clockwise // Whether perpendicular is clockwise or not
		? new Vector2(directionToPlayer.y, -directionToPlayer.x)
		: new Vector2(-directionToPlayer.y, directionToPlayer.x);

		Vector2 desiredDirection = directionToPlayer + (perpendicularDirection * _moveDirectionBias);

		desiredDirection.Normalize();

		if (_knockback.Velocity.magnitude > 0)
		{
			_velocity = desiredDirection + _knockback.Velocity;
		}
		else
		{
			_velocity = Vector2.Lerp(_velocity, desiredDirection * _speed, _turnSharpness * Time.fixedDeltaTime);
		}

		_rb2d.linearVelocity = _velocity;
	}

	private Player GetClosestPlayer()
	{
		Player closestPlayer = null;
		float closestDistance = float.MaxValue;
		Vector3 pixiePosition = transform.position;

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
}
