using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class PlayerCamera : NetworkBehaviour
{
	[field: Tooltip("How much padding, from the min and max points of the camera bounds to give to cover the whole frustum for the lightmap")]
	[field: SerializeField] public float MinMaxOffsetPadding { get; private set; }
	[field: SerializeField] public PolygonCollider2D WorldBoundary { get; private set; }

	private CinemachineConfiner2D _confiner;
	private BoxCollider2D _cameraFrustumCollider;
	private Camera _mainCamera;
	private CinemachineCamera _cinemachineCam;
	private Transform _originalFollowTarget;
	private float _originalOrthoSize;
	private NetworkObject _playerObject;
	private Vector3 _lastPlayerPosition;

	private Vector2Int _cachedMinCorner;
	private Vector2Int _cachedMaxCorner;
	
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
		if(_playerObject == null) return;
	
		if(_playerObject != null && _playerObject.transform.position != _lastPlayerPosition)
		{
			SetListenerToPlayer();
			_lastPlayerPosition = _playerObject.transform.position;
		}

        // Update the frustum collider in case the camera size has changed
        float verticalSize = _mainCamera.orthographicSize * 2;
        float horizontalSize = verticalSize * _mainCamera.aspect;
        _cameraFrustumCollider.size = new Vector2(horizontalSize, verticalSize);
        _cameraFrustumCollider.offset = Vector2.zero;

        // Check if the camera frustum bounds have changed, with padding
        Bounds bounds = _cameraFrustumCollider.bounds;
        int width = Mathf.CeilToInt(bounds.size.x + MinMaxOffsetPadding * 2);
        int height = Mathf.CeilToInt(bounds.size.y + MinMaxOffsetPadding * 2);
        Vector2 center = bounds.center;
        Vector2Int centerInt = Vector2Int.RoundToInt(center);
        Vector2Int currentMin = centerInt - new Vector2Int(width / 2, height / 2);
        Vector2Int currentMax = centerInt + new Vector2Int(width / 2, height / 2);

        if (currentMin != _cachedMinCorner || currentMax != _cachedMaxCorner)
        {
            _cachedMinCorner = currentMin;
            _cachedMaxCorner = currentMax;
            OnFrustumBoundsChanged();
        }
	}

	private void OnFrustumBoundsChanged()
	{
		// TODO: Implement logic when camera bounds change
		Lightmap.Instance.UpdateLightMapBounds(_cachedMinCorner, _cachedMaxCorner);
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
