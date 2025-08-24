using UnityEngine;

public class FoliageCollider : MonoBehaviour
{
    [field: SerializeField] public ParticleSystem DestroyVFXPrefab { get; private set; }

    public void DestroyFoliage()
    {
        if (DestroyVFXPrefab != null)
        {
            Instantiate(DestroyVFXPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }
}
