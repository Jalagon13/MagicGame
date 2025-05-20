using UnityEngine;

public class FoliageCollider : MonoBehaviour
{
    [field: SerializeField] public ParticleSystem DestroyVFXPrefab { get; private set; }

    private void OnDestroy()
    {
        if (DestroyVFXPrefab != null)
        {
            Instantiate(DestroyVFXPrefab, transform.position, Quaternion.identity);
        }
    }
}
