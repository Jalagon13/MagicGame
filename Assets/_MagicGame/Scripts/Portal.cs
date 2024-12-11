using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Portal : MonoBehaviour
{
    [SerializeField] private WorldInput _worldInput;
    [SerializeField] private WorldManager.EnvironmentID _destinationEnvironment;
	
    private BoxCollider2D _collider;
    private ResourceObject _resourceObject;
    private string _portalID;
	
    private void Awake()
    {
        _collider = GetComponent<BoxCollider2D>();
        _resourceObject = GetComponent<ResourceObject>();
    }
	
    private void Start()
    {
        _worldInput.OnInteractStarted += WorldInputDetector_OnInteractStarted;
        _resourceObject.OnBrokenByPlayer += ResourceAsset_OnBrokenByPlayer;
		
        // // Check if there exists portal data for this portal at this position
        // if(GameWorldManager.Instance.PortalDataPositionExistsAt(transform.position, out PortalData portalData))
        // {
        // 	// Fetch the PortalID and destinationID of that and update it here
        // 	SetPortalID(portalData.PortalID);
        // 	SetDestination(portalData.DestinationID);
        // 	SetDestructable(portalData.IsDestructable);
        // 	Debug.Log("meow");
        // }
        // else if(_resourceAsset.IsPlacedDownByPlayer())
        // {
        // 	// Register portal with its position and new ID 
        // 	SetPortalID(Guid.NewGuid().ToString());
        // 	Debug.Log("asset placed down by player");
        // 	GameWorldManager.Instance.LinkPortal(_destinationEnvironment, _portalID, transform.position);
        // }
        // else
        // {
        // 	// If this portal should not exist, and is not destructable, then delete it
        // 	Debug.Log("Destsroying portal");
        // 	Destroy(gameObject);
        // }
    }

    private void ResourceAsset_OnBrokenByPlayer(object sender, EventArgs e)
    {
        // GameWorldManager.Instance.UnLinkPortal(_portalID);
    }

    private void WorldInputDetector_OnInteractStarted(object sender, InputAction.CallbackContext e)
    {
        if(!_worldInput.GetMouseOverDetector()) return;
		
        // GameWorldManager.Instance.LoadEnvironment(_destinationEnvironment, _portalID);
    }
	
    public void SetDestructable(bool isDestructable)
    {
        _collider.enabled = isDestructable;
    }
	
    public void SetDestination(WorldManager.EnvironmentID id)
    {
        _destinationEnvironment = id;
    }
	
    private void SetPortalID(string id)
    {
        _portalID = id;
    }
	
    private void OnDestroy()
    {
        _worldInput.OnInteractStarted -= WorldInputDetector_OnInteractStarted;
        _resourceObject.OnBrokenByPlayer -= ResourceAsset_OnBrokenByPlayer;
		
        // Portal Unlink Logic
    }
}
