using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
	public void Initialize(ItemSO projectileItemSO)
	{
		if(projectileItemSO == null)
		{
			Debug.Log($"Projectile initialized with NULL itemSO");
		}
		else
		{
			Debug.Log($"Projectile initialized with itemSO: {projectileItemSO.name}");
			GetComponent<SpriteRenderer>().sprite = projectileItemSO.UiDisplay;
		}
	}
}
