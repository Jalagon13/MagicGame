using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PixieAI : MonoBehaviour
{
	[SerializeField] private float _speed;
	[SerializeField] private float _turnSharpness;
	[SerializeField] private float _moveDirectionBias;
	[SerializeField] private float _distanceThreshold = 0.5f; // Minimum distance the Pixie must move
	[SerializeField] private float _timeThreshold = 2f; // Time before doing something if Pixie didn't move enough
	[SerializeField] private float _fleeTime = 1.5f; 

	private Rigidbody2D _rb2d;
	private Player _closestPlayer;
	private Knockback _knockback;
	private Vector2 _velocity;
	private Vector2 _desiredDirection;
	private bool _clockwise = false;
	private bool _isFleeing = false; // Tracks if the Pixie is fleeing

	// Movement tracking
	private Vector2 _lastPosition;
	private float _timeNotMoved = 0f;

	private void Awake()
	{
		_rb2d = GetComponent<Rigidbody2D>();
		_knockback = GetComponent<Knockback>();
		_knockback.OnKnockbackStart += OnKnockbackStart;
		_lastPosition = transform.position; // Initialize last position
	}

	private void OnKnockbackStart(object sender, Knockback.KnockbackEventArgs e)
	{
		_clockwise = !_clockwise;
	}

	private void FixedUpdate()
	{
		if (_isFleeing) return; // Don't update movement if fleeing

		_closestPlayer = GetClosestPlayer();
		if (_closestPlayer == null || !_closestPlayer.gameObject.activeInHierarchy)
		{
			return;
		}

		Vector2 directionToPlayer = (_closestPlayer.transform.position - transform.position).normalized;

		Vector2 perpendicularDirection = _clockwise
			? new Vector2(directionToPlayer.y, -directionToPlayer.x)
			: new Vector2(-directionToPlayer.y, directionToPlayer.x);

		_desiredDirection = directionToPlayer + (perpendicularDirection * _moveDirectionBias);
		_desiredDirection.Normalize();

		PixieMovement(_desiredDirection);

		// Check movement every `_timeThreshold` interval
		_timeNotMoved += Time.fixedDeltaTime;
		if (_timeNotMoved >= _timeThreshold)
		{
			float distanceMoved = Vector2.Distance(_lastPosition, transform.position);

			if (distanceMoved < _distanceThreshold)
			{
				StartCoroutine(TryToFindDifferentPathToPlayer());
			}

			// Reset timer and update last known position
			_timeNotMoved = 0f;
			_lastPosition = transform.position;
		}
	}
	
	private void PixieMovement(Vector2 desiredDireciton)
	{
		if (_knockback.Velocity.magnitude > 0)
		{
			_velocity = desiredDireciton + _knockback.Velocity;
		}
		else
		{
			_velocity = Vector2.Lerp(_velocity, desiredDireciton * _speed, _turnSharpness * Time.fixedDeltaTime);
		}

		_rb2d.linearVelocity = _velocity;
	}

	private IEnumerator TryToFindDifferentPathToPlayer()
	{
		Debug.Log("Pixie is stuck! Fleeing...");
		_isFleeing = true;

		// Get direction AWAY from the player
		Vector2 fleeDirection = (_closestPlayer.transform.position - transform.position).normalized * -1;
		float elapsedTime = 0f;

		while (elapsedTime < _fleeTime)
		{
			PixieMovement(fleeDirection);
			elapsedTime += Time.deltaTime;
			yield return null;
		}

		_isFleeing = false;
		_clockwise = !_clockwise;
		Debug.Log("Pixie is returning to the player.");

		// Smoothly transition _moveDirectionBias from 1 to 0.5 over 1.5 seconds
		float transitionTime = 1.5f;
		float startBias = 1f;
		float endBias = 0.5f;
		float biasElapsed = 0f;
		float baseTurnSharpness = _turnSharpness;

		while (biasElapsed < transitionTime)
		{
			_moveDirectionBias = Mathf.Lerp(startBias, endBias, biasElapsed / transitionTime);
			_turnSharpness = Mathf.Lerp(10, baseTurnSharpness, biasElapsed / transitionTime);
			biasElapsed += Time.deltaTime;
			yield return null;
		}

		_moveDirectionBias = endBias; // Ensure final value is exactly 0.5
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