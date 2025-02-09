using System;
using UnityEngine;

public class Knockback : MonoBehaviour
{
	public event EventHandler<KnockbackEventArgs> OnKnockbackStart;
	public class KnockbackEventArgs : EventArgs
	{
		public Vector2 KnockBackerPosition;
	}

	[SerializeField] private bool _knockbackEnabled = true;
	public Vector2 Velocity;
	[SerializeField] private float _decayMult = 5f; // Higher = knockback fades out faster
	private float _minKnockback = 1;
	private float _maxKnockback = 100;
	private float _finalKnockback;
	public bool IsBeingKnockedBack;

	private void FixedUpdate()
	{
		ApplyVelocity();
	}

	public void ApplyKnockback(Vector2 knockerSourcePosition, float knockbackResist, float knockbackForce = -1)
	{
		if (!_knockbackEnabled) return;

		OnKnockbackStart?.Invoke(this, new KnockbackEventArgs
		{
			KnockBackerPosition = knockerSourcePosition
		});

		if (knockbackForce == -1) knockbackForce = _minKnockback;

		Vector2 direction = ((Vector2)transform.position - knockerSourcePosition).normalized;

		// Calculate knockback with resistance
		float finalKnockback = knockbackForce * (1 - knockbackResist);
		_finalKnockback = Mathf.Clamp(finalKnockback, _minKnockback, _maxKnockback);

		// Apply instant velocity change for knockback
		Debug.Log(_finalKnockback);
		Velocity = direction * _finalKnockback;
	}

	private void ApplyVelocity()
	{
		// Reduce knockback velocity over time
		Velocity = Vector2.Lerp(Velocity, Vector2.zero, _decayMult * Time.fixedDeltaTime);
		IsBeingKnockedBack = Velocity.magnitude > _finalKnockback * 0.75f;
		
		
		// if(_velocity == Vector2.zero) return;
		// Move the object using transform position
		// transform.position += (Vector3)(_velocity * Time.fixedDeltaTime);
		// GetComponent<Rigidbody2D>().MovePosition((Vector2)transform.position + (_velocity * Time.fixedDeltaTime));
	}
}