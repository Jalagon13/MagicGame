using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

public class Portal : MonoBehaviour
{
	[SerializeField] private WorldInput _worldInput;
	
	private BoxCollider2D _collider;
	private ResourceObject _resourceObject;
	
	private void Awake()
	{
		_collider = GetComponent<BoxCollider2D>();
		_resourceObject = GetComponent<ResourceObject>();
	}
	
	private void Start()
	{
		_worldInput.OnInteractStarted += WorldInputDetector_OnInteractStarted;
		_resourceObject.OnBrokenByPlayer += ResourceAsset_OnBrokenByPlayer;
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
	
	[Button("Load Forest")]
	public void LoadForest()
	{
		WorldManager.Instance.LoadEnvironment(WorldManager.EnvironmentID.Forest, transform.position);
	}
	

	[Button("Load Cave")]
	public void LoadCave()
	{
		WorldManager.Instance.LoadEnvironment(WorldManager.EnvironmentID.Cave, transform.position);
	}
	
	private void OnDestroy()
	{
		_worldInput.OnInteractStarted -= WorldInputDetector_OnInteractStarted;
		_resourceObject.OnBrokenByPlayer -= ResourceAsset_OnBrokenByPlayer;
		
		// Portal Unlink Logic
	}
}
