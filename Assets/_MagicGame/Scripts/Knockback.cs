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
	[SerializeField] private float _decayMult = 5f; // Higher = knockback fades out faster
	
	public Vector2 Velocity { get; private set; }
	private float _minKnockback = 5;
	private float _maxKnockback = 100;
	private float _finalKnockback;

	private void FixedUpdate()
	{
		ApplyVelocity();
	}

	public void ApplyKnockback(Vector2 knockerSourcePosition, float knockbackResist, float knockbackForce = -1, bool inverse = false)
	{
		if (!_knockbackEnabled) return;

		OnKnockbackStart?.Invoke(this, new KnockbackEventArgs
		{
			KnockBackerPosition = knockerSourcePosition
		});

		if (knockbackForce == -1) knockbackForce = _minKnockback;

		Vector2 direction = ((Vector2)transform.position - knockerSourcePosition).normalized;

		if (inverse) 
			direction *= -1;

		// Calculate knockback with resistance
		float finalKnockback = knockbackForce * (1 - knockbackResist);
		_finalKnockback = Mathf.Clamp(finalKnockback, _minKnockback, _maxKnockback);

		Velocity = direction * _finalKnockback;
	}

	private void ApplyVelocity()
	{
		// Reduce knockback velocity over time
		Velocity = Vector2.Lerp(Velocity, Vector2.zero, _decayMult * Time.fixedDeltaTime);
		
		if(Velocity.magnitude < 0.75f) Velocity = Vector2.zero;
	}
}