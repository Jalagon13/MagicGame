using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{	
	public static event EventHandler<PlayerIdEventArgs> OnAnyPlayerSpawned;
	public class PlayerIdEventArgs : EventArgs
	{
		public ulong PlayerId;
	}

	public static Player LocalClientInstance { get; private set; }
	
	[field: SerializeField] public CollectTag CollectTag { get; private set; }
	[SerializeField] private GameObject _breadCrumbPrefab;
	public Collider2D HitCollider { get; private set; }
	public bool IsPerformingSwing { get; set; }
	
	[SerializeField] private float _respawnTimerDuration;


	private Vector2 _spawnPoint;
	private BiomeType _spawnBiome;
	private Vector2Int _lastTilePosition;
	
	[SerializeField] private PlayerHand _playerHand;
	public PlayerHand PlayerHand => _playerHand;
	
	public NetworkVariable<int> SelectedItemIdNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<BiomeType> CurrentBiome { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	
	private ServerCharacter _serverCharacter;
	public ServerCharacter ServerCharacter => _serverCharacter;
	
	private PlayerNetworkVisibility _playerNetworkVisibility;
	public PlayerNetworkVisibility PlayerNetworkVisibility => _playerNetworkVisibility;
	
	private void Awake()
	{
		_serverCharacter = GetComponent<ServerCharacter>();
		_playerNetworkVisibility = GetComponent<PlayerNetworkVisibility>();
		HitCollider = GetComponent<Collider2D>();
	}
	
	public void OnNetworkSpawnLocalClientInitializations()
	{
		LocalClientInstance = this;
		CurrentBiome.Value = BiomeType.Forest; // For now all players will spawn in the forest
		_spawnBiome = BiomeType.Forest;
		_spawnPoint = transform.position;

		OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});

		Debug.Log($"{gameObject.name} spawned and initialized");
		// local player start up code here, maybe input
		GameInput.Instance.OnMove += GameInput_OnPlayerMove;
		GameInput.Instance.OnPrimaryAction += GameInput_OnPrimaryAction;
		HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnSelectedItemUpdated;
	}

	public override void OnNetworkDespawn()
	{
		if (IsClient && !_serverCharacter.Data.IsNpc && _serverCharacter.IsOwner)
		{
			GameInput.Instance.OnMove -= GameInput_OnPlayerMove;
			GameInput.Instance.OnPrimaryAction -= GameInput_OnPrimaryAction;
			HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnSelectedItemUpdated;
		}
	}

	private void Update()
	{
		if (IsOwner)
		{
			Vector2Int newTilePosition = new(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
			if (newTilePosition != _lastTilePosition)
			{
				_lastTilePosition = newTilePosition;
				SpawnBreadCrumbServerRpc(_lastTilePosition);
			}
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnBreadCrumbServerRpc(Vector2Int spawnPos, RpcParams rpcParams = default)
	{
		GameObject breadCrumb = Instantiate(_breadCrumbPrefab, new Vector2(spawnPos.x + 0.5f, spawnPos.y + 0.5f), Quaternion.identity);
		breadCrumb.GetComponent<BreadCrumb>().InitializeBreadCrumb(CurrentBiome.Value);
		GameManager.Instance.InvokeSpawnBreadCrumbEvent(breadCrumb);
	}

	private void GameInput_OnPrimaryAction(object sender, GameInput.OnPrimaryOrSecondaryActionEventArgs e)
	{
		
	}

	private void GameInput_OnPlayerMove(object sender, InputAction.CallbackContext e)
	{
		if (_serverCharacter.LifeState == LifeState.Alive)
		{
			var desiredDirection = e.ReadValue<Vector2>();
			if(desiredDirection == Vector2.zero)
			{
				_serverCharacter.Movement.StartIdle();
			}
			else
			{
				_serverCharacter.Movement.StartMovement(desiredDirection);
			}
		}
	}

	private void HotbarManager_OnSelectedItemUpdated(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		if (IsOwner)
		{
			// NTFS: Network variables onvaluechanged is only executed if the value is different from the current value
			SelectedItemIdNetworkVariable.Value = e.SelectedItemId;
		}
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void RespawnPlayerClientRpc(RpcParams rpcParams = default)
	{
		transform.SetPositionAndRotation(_spawnPoint, Quaternion.identity);

		if (CurrentBiome.Value != _spawnBiome)
		{
			WorldManager.Instance.LoadBiome(_spawnBiome, _spawnPoint);
		}

		// OnRespawn?.Invoke(this, new PlayerIdEventArgs
		// {
		// 	PlayerId = OwnerClientId
		// });
	}
}
