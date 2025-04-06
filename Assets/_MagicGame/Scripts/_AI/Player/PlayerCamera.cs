using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerCamera : NetworkBehaviour
{
	[field: SerializeField] public PolygonCollider2D WorldBoundary { get; private set; }

	private CinemachineConfiner2D _confiner;
	private BoxCollider2D _cameraFrustumCollider;
	private Camera _mainCamera;
	private CinemachineCamera _cinemachineCam;
	private Transform _originalFollowTarget;
	private float _originalOrthoSize;
	private NetworkObject _playerObject;
	private Vector3 _lastPlayerPosition;
	
	private void Awake() 
	{
		_confiner = GetComponent<CinemachineConfiner2D>();
		_cameraFrustumCollider = GetComponent<BoxCollider2D>();
		
		_cinemachineCam = GetComponent<CinemachineCamera>();
		_originalFollowTarget = _cinemachineCam.Follow;
		_originalOrthoSize = _cinemachineCam.Lens.OrthographicSize;
		_cinemachineCam.enabled = false;

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
	
	private void Update()
	{
		if(_playerObject != null && _playerObject.transform.position != _lastPlayerPosition)
		{
			SetListenerToPlayer();
			_lastPlayerPosition = _playerObject.transform.position;
		}
	}

	private void RegisterCameraToPlayer(ulong clientId)
	{
		if(NetworkManager.LocalClientId != clientId) return;
		
		_playerObject = NetworkManager.ConnectedClients[clientId].PlayerObject;
		_cinemachineCam.Follow = _playerObject.transform;
		_confiner.BoundingShape2D = WorldBoundary;
		_cinemachineCam.enabled = true;
		Debug.Log($"Player object: {_playerObject}");
		SetListenerToPlayer();
	}
	
	private void SetListenerToPlayer()
	{
		var attributes = new FMOD.ATTRIBUTES_3D
		{
			position = new FMOD.VECTOR
			{
				x = _playerObject.transform.position.x,
				y = _playerObject.transform.position.y,
				z = _playerObject.transform.position.z
			}
		};
		RuntimeManager.StudioSystem.setListenerAttributes(0, attributes);
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
