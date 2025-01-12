using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
	private CinemachineConfiner2D _confiner;
	private BoxCollider2D _cameraFrustumCollider;
	private Camera _mainCamera;
	private CinemachineCamera _cinemachineCam;
	private Transform _originalFollowTarget;
	private float _originalOrthoSize;
	
	private void Awake() 
	{
		_confiner = GetComponent<CinemachineConfiner2D>();
		_cameraFrustumCollider = GetComponent<BoxCollider2D>();
		
		_cinemachineCam = GetComponent<CinemachineCamera>();
		_originalFollowTarget = _cinemachineCam.Follow;
		_originalOrthoSize = _cinemachineCam.Lens.OrthographicSize;
		
		_mainCamera = Camera.main;
		
		if(NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback += RegisterCameraToPlayer;
		}
	}

	// NTFS: Change this dynamically when camera is widened or narrowed
	private void Start()
	{
		float verticalSize = _mainCamera.orthographicSize * 2;
		float horizontalSize = verticalSize * _mainCamera.aspect;
		_cameraFrustumCollider.size = new Vector2(horizontalSize, verticalSize);
		_cameraFrustumCollider.offset = Vector2.zero;
	}

	private void RegisterCameraToPlayer(ulong clientId)
	{
		if(NetworkManager.LocalClientId != clientId) return;
		
		_cinemachineCam.Follow = NetworkManager.ConnectedClients[clientId].PlayerObject.transform;
	}
	
	public override void OnDestroy() 
	{
		if(NetworkManager != null)
		{
			NetworkManager.OnClientConnectedCallback -= RegisterCameraToPlayer;
		}
		
		base.OnDestroy();
	}
}
