using UnityEngine;

public class DamageCollider : MonoBehaviour
{
	[SerializeField] private int _damageAmount;

	private void OnTriggerStay2D(Collider2D other)
	{
		if(other == transform.root.GetComponent<Collider2D>()) return;
	
		if(other.TryGetComponent(out IHasHealth iHasHealth))
		{
			iHasHealth.ApplyDamage(_damageAmount, transform.root.position);
		}
	}
}