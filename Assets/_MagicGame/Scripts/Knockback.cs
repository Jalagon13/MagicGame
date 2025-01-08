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
	private Coroutine _kbCoroutine;
	private Rigidbody2D _rb2d;
	
	public bool IsBeingKnockedBack { get; private set; }
	
	public void ApplyKnockback(Rigidbody2D rb2d, Vector2 knockerSourcePosition, float knockbackForce = 20f)
	{
		if(!_knockbackEnabled) return;
		
		_rb2d = rb2d;
		var direction = (Vector2)transform.position - knockerSourcePosition;
		
		_kbCoroutine = StartCoroutine(KnockbackRoutine(knockerSourcePosition, direction, knockbackForce));
	}
	
	public void CancelKnockback()
	{
		_rb2d.linearVelocity = Vector2.zero;
		StopCoroutine(_kbCoroutine);
	}
	
	private IEnumerator KnockbackRoutine(Vector2 knockerSourcePosition, Vector2 direction, float knockbackForce)
	{
		IsBeingKnockedBack = true;
		
		OnKnockbackStart?.Invoke(this, new KnockbackEventArgs
		{
			KnockBackerPosition = knockerSourcePosition
		});
		
		_rb2d.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
		
		while(_rb2d.linearVelocity.magnitude > 0.15f)
		{
			yield return new WaitForFixedUpdate();
		}
		
		IsBeingKnockedBack = false;
		
		OnKnockbackEnd?.Invoke(this, new KnockbackEventArgs
		{
			KnockBackerPosition = knockerSourcePosition
		});
	}
}
