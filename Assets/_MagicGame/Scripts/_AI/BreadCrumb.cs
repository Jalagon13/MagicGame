using System;
using UnityEngine;


namespace ProjectWizard
{
	public class BreadCrumb : MonoBehaviour
	{
	    [SerializeField] private float _lifetimeDuration = 3f;

	    private Collider2D _breadCrumbCollider;
	    private BiomeType _biome;
	    private float _expirationTime;

	    public BiomeType Biome => _biome;
	    public float RemainingLifeTime => Mathf.Max(_expirationTime - Time.time, 0f);

	    public void InitializeBreadCrumb(BiomeType biome)
	    {
	        _breadCrumbCollider = GetComponent<Collider2D>();
	        _biome = biome;
	        _expirationTime = Time.time + _lifetimeDuration;

	        // Check for an existing breadcrumb at the same position
	        Collider2D[] colliders = Physics2D.OverlapPointAll(transform.position);

	        foreach (Collider2D collider in colliders)
	        {
	            if (collider != _breadCrumbCollider) // Ignore self
	            {
	                BreadCrumb existingBreadCrumb = collider.GetComponent<BreadCrumb>();

	                if (existingBreadCrumb != null && existingBreadCrumb.Biome == _biome) // Use property here
	                {
	                    existingBreadCrumb.Refresh(); // Refresh existing breadcrumb
	                    Destroy(gameObject); // Destroy this breadcrumb
	                    return;
	                }
	            }
	        }
	    }

	    private void Update()
	    {
	        if (Time.time >= _expirationTime)
	        {
	            Destroy(gameObject);
	        }
	    }

	    public void Refresh()
	    {
	        _expirationTime = Time.time + _lifetimeDuration;
	    }
	}
}