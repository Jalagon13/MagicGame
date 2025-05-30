using System;
using UnityEngine;

public class Knockback
{
	public event EventHandler<KnockbackEventArgs> OnKnockbackStart;
	public event EventHandler OnKnockbackEnd;
	public class KnockbackEventArgs : EventArgs
	{
		public Vector2 KnockBackerPosition;
	}
	
	public Vector2 Velocity { get; private set; }
	public bool KnockbackActive => Velocity != Vector2.zero;

	private bool _knockbackEnabled = true;
	private float _decayMult = 5f; // Higher = knockback fades out faster
	private float _minKnockback = 0;
	private float _maxKnockback = 100;
	private float _finalKnockback;
    private ServerCharacter _serverCharacter;

    public Knockback(ServerCharacter serverCharacter)
	{
		_serverCharacter = serverCharacter;
	}

	public void UpdateKnockback(float fixedDeltaTime)
	{
		if(Velocity == Vector2.zero) return;
		
		Velocity = Vector2.Lerp(Velocity, Vector2.zero, _decayMult * fixedDeltaTime);
		
		if (Velocity.magnitude < 0.75f)
		{
			Velocity = Vector2.zero;
			OnKnockbackEnd?.Invoke(this, EventArgs.Empty);
		}
	}

	public void ApplyKnockbackCustomDirection(Vector2 direction, float knockbackForce)
	{
		if (!_knockbackEnabled) return;

		OnKnockbackStart?.Invoke(this, new KnockbackEventArgs { } );

		float finalKnockback = knockbackForce * (1 - _serverCharacter.Data.KnockbackResist);
		_finalKnockback = Mathf.Clamp(finalKnockback, _minKnockback, _maxKnockback);

		Velocity = direction * _finalKnockback;
	}
	
	public void ApplyKnockback(Vector2 knockerSourcePosition, float knockbackForce = -1, bool inverse = false)
	{
		if (!_knockbackEnabled) return;

		OnKnockbackStart?.Invoke(this, new KnockbackEventArgs
		{
			KnockBackerPosition = knockerSourcePosition
		});
		if (knockbackForce == -1) knockbackForce = _minKnockback;

		Vector2 direction = ((Vector2)_serverCharacter.transform.position - knockerSourcePosition).normalized;

		if (inverse) 
			direction *= -1;

		// Calculate knockback with resistance
		float finalKnockback = knockbackForce * (1 - _serverCharacter.Data.KnockbackResist);
		_finalKnockback = Mathf.Clamp(finalKnockback, _minKnockback, _maxKnockback);
		_decayMult = Mathf.Lerp(10, 1, _finalKnockback / _maxKnockback);

		Velocity = direction * _finalKnockback;
	}
}