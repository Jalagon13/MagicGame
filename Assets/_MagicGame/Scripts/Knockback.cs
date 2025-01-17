using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockback : MonoBehaviour
{
	public event EventHandler<KnockbackEventArgs> OnKnockbackEnd;
	public event EventHandler<KnockbackEventArgs> OnKnockbackStart;

	public class KnockbackEventArgs : EventArgs
	{
		public Vector2 KnockBackerPosition;
	}

	[SerializeField] private bool _knockbackEnabled = true;
	[SerializeField] private float _knockbackDuration = 0.2f; // Total time for knockback effect
	private Coroutine _kbCoroutine;
	private Rigidbody2D _rb2d;
	
	public bool IsBeingKnockedBack { get; private set; }
	
	public void ApplyKnockback(Rigidbody2D rb2d, Vector2 knockerSourcePosition, float knockbackForce = 20f)
	{
		if (!_knockbackEnabled) return;

		_rb2d = rb2d;
		var direction = (Vector2)transform.position - knockerSourcePosition;

		_kbCoroutine = StartCoroutine(KnockbackRoutine(knockerSourcePosition, direction, knockbackForce));
	}
	
	public void CancelKnockback()
	{
		_rb2d.linearVelocity = Vector2.zero;
		StopCoroutine(_kbCoroutine);
		IsBeingKnockedBack = false;
	}
	
	private IEnumerator KnockbackRoutine(Vector2 knockerSourcePosition, Vector2 direction, float knockbackForce)
	{
		IsBeingKnockedBack = true;
		
		OnKnockbackStart?.Invoke(this, new KnockbackEventArgs
		{
			KnockBackerPosition = knockerSourcePosition
		});

		// Apply the knockback force
		_rb2d.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
		
		float knockbackTimer = _knockbackDuration; // Set the timer to knockbackDuration

		// While the knockback effect is active
		while (knockbackTimer > 0f)
		{
			knockbackTimer -= Time.fixedDeltaTime; // Decrement the timer
			// You can still add a velocity check here if needed:
			if (_rb2d.linearVelocity.magnitude < 0.15f)
			{
				break; // Exit early if the velocity is sufficiently low
			}

			yield return new WaitForFixedUpdate();
		}

		// End knockback after the duration or early if velocity is low
		IsBeingKnockedBack = false;
		
		OnKnockbackEnd?.Invoke(this, new KnockbackEventArgs
		{
			KnockBackerPosition = knockerSourcePosition
		});
	}
}