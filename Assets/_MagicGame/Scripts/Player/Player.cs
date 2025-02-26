using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour, IHasHealth
{	
	public static event EventHandler<PlayerIdEventArgs> OnAnyPlayerSpawned;
	public class PlayerIdEventArgs : EventArgs
	{
		public ulong PlayerId;
	}

	public static Player LocalClientInstance { get; private set; }
	
	public event EventHandler<PlayerIdEventArgs> OnRespawn;
	public event EventHandler<PlayerIdEventArgs> OnDeath;
	public event EventHandler<OnDamagedEventArgs> OnDamaged;
	public class OnDamagedEventArgs : EventArgs
	{
		public int DamageAmount;
		public Vector2 DamagerPosition;
	}
	
	public event EventHandler<IHasHealth.OnHealthUpdatedEventArgs> OnHealthUpdated;
	public class OnStatUpdatedEventArgs : EventArgs
	{
		public int PreviousValue;
		public int NewValue;
		public int MaxValue;
	}
	
	[field: SerializeField] public Transform ProjectileSpawnPointTf { get; private set; }
	[field: SerializeField] public PlayerHand MainHand { get; private set; }
	[field: SerializeField] public CollectTag CollectTag { get; private set; }
	[SerializeField] private GameObject _breadCrumbPrefab;
	public PlayerStats PlayerStats { get; private set; }
	public NetworkVariable<int> MainHandItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<BiomeType> CurrentPlayerBiome { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public BiomeType Biome { get { return CurrentPlayerBiome.Value; } }
	public Collider2D HitCollider { get; private set; }
	public bool IsPerformingSwing { get; set; }
	
	[SerializeField] private float _respawnTimerDuration;
	[field: SerializeField] public float IFrameLength { get; private set; } = 0.67f;
	[Range(0, 100), SerializeField] private float _knockbackResist;
	[SerializeField] private List<InventoryItem> _startingItems = new();
	[SerializeField] private bool _spawnWandItems;
	[SerializeField] private List<WandInventoryItem> _startingWandItems = new();

	public NetworkVariable<int> HealthNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<bool> ExecutingIFrames { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<bool> PvpEnabled { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	
	public Knockback _playerKnockback { get; private set; }
	private Timer _respawnTimer;
	private Vector2 _spawnPoint;
	private BiomeType _spawnBiome;
	private Vector2Int _lastTilePosition;
	
	private void Awake()
	{
		PlayerStats = GetComponent<PlayerStats>();
		HitCollider = GetComponent<Collider2D>();
		HealthNetworkVariable.OnValueChanged += HealthNetworkVariable_OnValueChanged;
		
		_playerKnockback = GetComponent<Knockback>();
		_respawnTimer = new(_respawnTimerDuration);
	}

	public override void OnNetworkSpawn()
	{
		gameObject.name = $"Player_{OwnerClientId}";
		
		if(IsOwner)
		{
			LocalClientInstance = this;
			
			CurrentPlayerBiome.Value = BiomeType.Forest; // For now all players will spawn in the forest
			_spawnBiome = BiomeType.Forest;
			_spawnPoint = transform.position;
			
			HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnMainHandSlotUpdated;
			GameInput.Instance.OnSpaceStarted += DashTest;
		}
		
		OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
		
		if(IsServer)
		{
			HealthNetworkVariable.Value = PlayerStats.StartingPlayerHealth;
			ExecutingIFrames.Value = false;
		}
	}

	private IEnumerator Start()
	{
		yield return new WaitForEndOfFrame();
	
		if (IsOwner)
		{
			if(_spawnWandItems)
			{
				foreach (WandInventoryItem wandInvItem in _startingWandItems)
				{
					if (wandInvItem.Item is not WandItemSO)
					{
						Debug.LogWarning($"{wandInvItem.Item} is not a wand. skipping it");
						continue;
					}

					WandItemSO wandItemSO = wandInvItem.Item as WandItemSO;
					WandInventoryItem wandItemToAdd = (WandInventoryItem)wandItemSO.CreateInventoryItem(1);

					for (int i = 0; i < wandInvItem.MagicArray.Length; i++)
					{
						if (wandInvItem.MagicArray[i] is MagicItemSO)
						{
							if (i < wandItemSO.Capacity)
							{
								wandItemToAdd.MagicArray[i] = wandInvItem.MagicArray[i];
							}
							else
							{
								Debug.LogWarning($"{wandInvItem.MagicArray[i].Name} being skipped because it is out of the index of {wandItemSO.Name}'s Capacity ({wandItemSO.Capacity})");
							}
						}
					}

					InventoryManager.Instance.AddItem(wandItemToAdd);
					yield return new WaitForEndOfFrame();
				}
			}
			

			foreach (InventoryItem item in _startingItems)
			{
				InventoryItem itemToAdd = item.Item.CreateInventoryItem(item.Quantity);
				InventoryManager.Instance.AddItem(itemToAdd);
				yield return new WaitForEndOfFrame();
			}
		}
	}

	private void Update()
	{
		if(IsDead() && NetworkManager.LocalClientId == OwnerClientId)
		{
			_respawnTimer.Tick(Time.deltaTime);
		}

		if(IsOwner)
		{
		    Vector2Int newTilePosition = new(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y));
			if(newTilePosition != _lastTilePosition)
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
		breadCrumb.GetComponent<BreadCrumb>().InitializeBreadCrumb(CurrentPlayerBiome.Value);
		Debug.Log($"player {rpcParams.Receive.SenderClientId} spawning breadcrumb. sender pos: {transform.position}, tilePos: {spawnPos}");
		GameManager.Instance.InvokeSpawnBreadCrumbEvent(breadCrumb);
	}

	#region Life Cycle Functions

	public void TogglePvp(bool pvpEnabled)
	{
		PvpEnabled.Value = pvpEnabled;
		Debug.Log($"Pvp enabled: {PvpEnabled.Value}");
	}

	public void ApplyDamage(int damage, Vector2 damagerPosition, int knockbackForce)
	{
		if (IsDead()) return;

		ApplyPlayerDamageServerRpc(OwnerClientId, damage, damagerPosition, knockbackForce);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void ApplyPlayerDamageServerRpc(ulong damagePlayerId, int damageAmount, Vector2 damagerPosition, int knockbackForce)
	{
		if (!NetworkManager.ConnectedClients.ContainsKey(damagePlayerId)) return;
	
		NetworkObject playerNetworkObject = NetworkManager.ConnectedClients[damagePlayerId].PlayerObject;
		
		if(playerNetworkObject.GetComponent<Player>().ExecutingIFrames.Value) return;
		
		int defense = playerNetworkObject.GetComponent<Player>().PlayerStats.PlayerDefense;
		int damageReduction = defense / 2;
		int finalDamage = Mathf.Max(1, damageAmount - damageReduction); 
		
		playerNetworkObject.GetComponent<Player>().HealthNetworkVariable.Value = Mathf.Max(0, HealthNetworkVariable.Value - finalDamage);
		playerNetworkObject.GetComponent<Player>().ExecuteIFrames();
		
		GameManager.Instance.PlayDamageNumbers(damageAmount, transform.position, CurrentPlayerBiome.Value);
		
		bool isPlayerDead = HealthNetworkVariable.Value <= 0;
		
		OnPlayerHealthChangedClientRpc(finalDamage, isPlayerDead, damagerPosition, knockbackForce, RpcTarget.Single(damagePlayerId, RpcTargetUse.Persistent));
	}
	
	public void ExecuteIFrames() => StartCoroutine(IframeRoutine());
	
	private IEnumerator IframeRoutine()
	{
		ExecutingIFrames.Value = true;
		
		yield return new WaitForSeconds(IFrameLength);
		
		ExecutingIFrames.Value = false;
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void OnPlayerHealthChangedClientRpc(int finalDamage, bool isKilled, Vector2 damagerPosition, float knockbackForce, RpcParams rpcParams = default)
	{
		if(isKilled)
		{
			Debug.Log($"[Client {NetworkManager.LocalClientId}] {gameObject.name} is dead!");

			_respawnTimer.Reset();
			_respawnTimer.OnTimerEnd += RespawnPlayer;

			OnDeath?.Invoke(this, new PlayerIdEventArgs
			{
				PlayerId = OwnerClientId
			});
		}
		else
		{
			Debug.Log($"[Client {NetworkManager.LocalClientId}] Applied {finalDamage} Damage to {gameObject.name}!");

			SoundManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerDamaged, transform.position);
			_playerKnockback.ApplyKnockback(damagerPosition, _knockbackResist, knockbackForce); 
			
			OnDamaged?.Invoke(this, new OnDamagedEventArgs
			{
				DamagerPosition = damagerPosition,
				DamageAmount = finalDamage
			});
		}
	}

	private void RespawnPlayer(object sender, EventArgs e)
	{
		_respawnTimer.OnTimerEnd -= RespawnPlayer;

		RespawnPlayerServerRpc(OwnerClientId, PlayerStats.StartingPlayerHealth);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RespawnPlayerServerRpc(ulong respawnerId, int healthToRespawnWith)
	{
		HealthNetworkVariable.Value = healthToRespawnWith;

		RespawnPlayerClientRpc(RpcTarget.Single(respawnerId, RpcTargetUse.Persistent));
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void RespawnPlayerClientRpc(RpcParams rpcParams = default)
	{
		transform.SetPositionAndRotation(_spawnPoint, Quaternion.identity);

		if (CurrentPlayerBiome.Value != _spawnBiome)
		{
			WorldManager.Instance.LoadBiome(_spawnBiome, _spawnPoint, false);
		}

		OnRespawn?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
	}

	#endregion

	private void DashTest(object sender, EventArgs e)
	{
		if (Pointer.IsOverUI() || ObjectManager.Instance.TryToFindWorldObject(Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), out WorldObject wo)) return;
		_playerKnockback.ApplyKnockback(ActionManager.MouseWorldPosition, _knockbackResist, 30, true);
	}

	private void HealthNetworkVariable_OnValueChanged(int previousValue, int newValue)
	{
		OnHealthUpdated?.Invoke(this, new IHasHealth.OnHealthUpdatedEventArgs
		{
			PreviousValue = previousValue,
			NewValue = newValue,
			MaxValue = PlayerStats.StartingPlayerHealth
		});
	}

	private void HotbarManager_OnMainHandSlotUpdated(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		if(IsOwner)
		{
			// NTFS: network variables onvaluechanged is only executed if the value is different
			if(e.MainHandItemIndex == -1)
			{
				MainHandItemIndexNetworkVariable.Value = -1;
			}
			else
			{
				MainHandItemIndexNetworkVariable.Value = e.MainHandItemIndex;
			}
		}
	}
	
	public bool IsDead()
	{
		return HealthNetworkVariable.Value <= 0;
	}
	
	public bool IsHoldingAWand()
	{
		ItemSO mainHandItem = GameManager.Instance.GetItemSOFromItemId(MainHandItemIndexNetworkVariable.Value);
		
		return mainHandItem != null && (mainHandItem is SpellBookItemSO || mainHandItem is WandItemSO);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		
		HealthNetworkVariable.OnValueChanged -= HealthNetworkVariable_OnValueChanged;
		
		if(IsOwner)
		{
			HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnMainHandSlotUpdated;
		}
	}
}
