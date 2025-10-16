using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;


namespace ProjectTinker
{
	public class ResourceFeedbacks : MonoBehaviour
	{
	    private static WaitForSeconds _waitForSecondsBeforeDespawn = new(4f);
    
	    [SerializeField] 
	    private ResourceObject _resourceObject;

	    [SerializeField] 
	    private MMF_Player _destroyFeedbacks, _hitFeedbacks;
    
	    [SerializeField] 
	    private GameObject _visuals;

	    [SerializeField]
	    private List<Gibfab> _gibfabs;

	    private Collider2D _resourceObjectCollider, _wallCollider;

	    void Awake()
	    {
	        _resourceObjectCollider = transform.parent.GetComponent<Collider2D>();
	        _wallCollider = transform.parent.GetChild(0).GetComponent<Collider2D>();
	    }

	    public void PlayHitFeedback()
	    {
	        _hitFeedbacks.PlayFeedbacks();
	    }

	    public void PlayClientDestructionSequence()
	    {
	        StartCoroutine(ClientDestructionSequence());
	    }

	    // TODO: Make a sub class for the tree and create custom gib feedback stuff in there
	    private IEnumerator ClientDestructionSequence()
	    {
	        SoundManager.Instance.PlayOneShot(_resourceObject.Data.ResourceDestroyed, transform.position);
	        Lightmap.Instance.UpdateLightMap();

	        _visuals.SetActive(false);
	        _resourceObjectCollider.enabled = false;
	        _wallCollider.enabled = false;

	        float startHeight = 0.5f;
	        float heightStep = .75f;
	        float currentHeight = startHeight;

	        int gibCount = _gibfabs.Count;
	        float baseAngleStep = 360f / gibCount; // evenly spaced angles
	        float randomOffset = Random.Range(30f, 60f); // same offset applied to all

	        for (int i = 0; i < gibCount; i++)
	        {
	            // Calculate angle in radians, add the random offset
	            float angle = (baseAngleStep * i + randomOffset) * Mathf.Deg2Rad;

	            // Unit direction based on angle
	            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

	            // Random velocity magnitude and initial upward speed
	            float randomVelocityMagnitude = Random.Range(1f, 5f);
	            float randomInitialUpwardSpeed = Random.Range(0f, 0.1f);

	            // Launch gib with steadily increasing height
	            _gibfabs[i].LaunchGib(randomInitialUpwardSpeed, currentHeight, direction * randomVelocityMagnitude);

	            currentHeight += heightStep;
	        }

	        _destroyFeedbacks.PlayFeedbacks();

	        yield return _waitForSecondsBeforeDespawn;
        
	        Destroy(_resourceObject.gameObject);
	    }
	}

}