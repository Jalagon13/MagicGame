using UnityEngine;

public class DamageCollider : MonoBehaviour
{
	[SerializeField] private int _damageAmount;
	[SerializeField] private int _knockbackForce;

	private void OnTriggerStay2D(Collider2D other)
	{
		if(other == transform.root.GetComponent<Collider2D>()) return;
	
		if(other.TryGetComponent(out IHasHealth iHasHealth))
		{
			iHasHealth.ApplyDamage(_damageAmount, transform.parent.position, _knockbackForce);
		}
	}
}