using System;
using System.Collections.Generic;
using UnityEngine;

public class DamageCollider : MonoBehaviour
{
	public event EventHandler<OnDamageEventArgs> OnDamage;
	public class OnDamageEventArgs : EventArgs
	{
		public Collider2D ColliderDamaged;
	}

	[field: SerializeField] public int DamageAmount { get; set; }
	[field: SerializeField] public int KnockbackForce { get; set; }
	public List<Collider2D> DamageExceptionColliders { get; private set; } = new();
	
	private void OnTriggerStay2D(Collider2D other)
	{
		if(!other.TryGetComponent(out IHasHealth iHasHealth) || ColliderAnException(other)) return; 
	
		iHasHealth.ApplyDamage(DamageAmount, transform.parent.position, KnockbackForce);
		OnDamage?.Invoke(this, new OnDamageEventArgs
		{
			ColliderDamaged = other
		});
	}
	
	public void AddDamageExceptionCollider(Collider2D col)
	{
		if(!DamageExceptionColliders.Contains(col))
		{
			DamageExceptionColliders.Add(col);
		}
	}
	
	private bool ColliderAnException(Collider2D col)
	{
		return DamageExceptionColliders.Contains(col);
	}
}