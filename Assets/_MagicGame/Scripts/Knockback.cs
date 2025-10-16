using System;
using UnityEngine;


namespace ProjectTinker
{
	public class Knockback
	{
		// DefaultDecayMult: Higher = knockback fades out faster
		private const float DefaultDecayMult = 5f, MinKnockbackForce = 0f, MaxKnockbackForce = 100f, KnockbackEndThreshold = 0.75f;

		public event EventHandler<KnockbackEventArgs> OnKnockbackStart;
		public event EventHandler OnKnockbackEnd;
		public class KnockbackEventArgs : EventArgs
		{
			public Vector2 KnockBackerPosition;
		}
	
		public Vector2 Velocity { get; private set; }

		private bool _knockbackEnabled = true;
		private float _decayMult = DefaultDecayMult, _minKnockback = MinKnockbackForce, _maxKnockback = MaxKnockbackForce, _finalKnockback; 
	    private ServerCharacter _serverCharacter;
    
		private bool _isKnockbackActive;
		public bool KnockbackActive => _isKnockbackActive;

	    public Knockback(ServerCharacter serverCharacter)
		{
			_serverCharacter = serverCharacter;
		}

		public void UpdateKnockback(float fixedDeltaTime)
		{
			if (!_isKnockbackActive) return;
		
			Velocity = Vector2.Lerp(Velocity, Vector2.zero, _decayMult * fixedDeltaTime);
		
			if (Velocity.magnitude < KnockbackEndThreshold)
			{
				Velocity = Vector2.zero;
				_isKnockbackActive = false;
				OnKnockbackEnd?.Invoke(this, EventArgs.Empty);
			}
		}

		public void ApplyKnockbackCustomDirection(Vector2 direction, float knockbackForce)
		{
			if (!_knockbackEnabled) return;

			OnKnockbackStart?.Invoke(this, new KnockbackEventArgs { } );

			float finalKnockback = knockbackForce * (1 - (_serverCharacter != null ? _serverCharacter.Data.KnockbackResist : 0));
			_finalKnockback = Mathf.Clamp(finalKnockback, _minKnockback, _maxKnockback);

			_isKnockbackActive = true;
			Velocity = direction * _finalKnockback;
		}
	
		// ServerCharacter must be set before calling this method
		public void ApplyKnockback(Vector2 knockerSourcePosition, float knockbackForce = -1, bool inverse = false)
		{
			if (!_knockbackEnabled || _serverCharacter == null) return;
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

			_isKnockbackActive = true;
			Velocity = direction * _finalKnockback;
		}
	}
}