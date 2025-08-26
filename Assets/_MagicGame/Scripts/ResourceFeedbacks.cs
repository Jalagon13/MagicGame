using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;

public class ResourceFeedbacks : MonoBehaviour
{
    [SerializeField] 
    private ResourceObject _resourceObject;

    [SerializeField] 
    private MMF_Player _destroyFeedbacks, _hitFeedbacks;
    
    [SerializeField] 
    private GameObject _visuals;
    
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

    private IEnumerator ClientDestructionSequence()
    {
        Debug.Log($"Resource Destruction Seuqnce started");
        SoundManager.Instance.PlayOneShot(_resourceObject.Data.ResourceDestroyed, transform.position);
        Lightmap.Instance.UpdateLightMap();
        
        _visuals.SetActive(false);
        _resourceObjectCollider.gameObject.SetActive(false);
        _wallCollider.gameObject.SetActive(false);
        _destroyFeedbacks.PlayFeedbacks(); 

        yield return new WaitForSeconds(4f);
        Destroy(gameObject);
    }
}
