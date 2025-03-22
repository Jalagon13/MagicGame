using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
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
	
	public NetworkVariable<int> SelectedItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<BiomeType> CurrentPlayerBiome { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<bool> PvpEnabled { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	
	[field: SerializeField] public PlayerHand MainHand { get; private set; }
	[field: SerializeField] public PlayerVisuals PlayerVisuals { get; private set; }
	[field: SerializeField] public CollectTag CollectTag { get; private set; }
	[SerializeField] private GameObject _breadCrumbPrefab;
	public PlayerStats PlayerStats { get; private set; }
	public BiomeType Biome { get { return CurrentPlayerBiome.Value; } }
	public Collider2D HitCollider { get; private set; }
	public bool IsPerformingSwing { get; set; }
	
	[SerializeField] private float _respawnTimerDuration;
	[Range(0, 100), SerializeField] private float _knockbackResist;
	[SerializeField] private bool _spawnWandItems;
	[SerializeField] private List<WandInventoryItem> _startingWandItems = new();
	[SerializeField] private List<InventoryItem> _startingItems = new();

	public Knockback PlayerKnockback { get; private set; }
	public NetworkHealthState HealthState { get; private set; }
	public PlayerStateMachine StateMachine { get; private set; }

	private Timer _respawnTimer;
	private Vector2 _spawnPoint;
	private BiomeType _spawnBiome;
	private Vector2Int _lastTilePosition;
	
	private void Awake()
	{
		PlayerStats = GetComponent<PlayerStats>();
		HitCollider = GetComponent<Collider2D>();
		PlayerKnockback = GetComponent<Knockback>();
		HealthState = GetComponent<NetworkHealthState>();
		StateMachine = GetComponent<PlayerStateMachine>();

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
			
			HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnSelectedItemUpdated;
		}
		
		OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
	}

	private IEnumerator Start()
	{
		HealthState.OnHitPointsDamaged += OnPlayerDamaged;
		HealthState.OnHitPointsDepleted += OnPlayerDeath;
		HealthState.OnHitPointsReplenished += OnPlayerRecovery;

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
		if (HealthState.IsDead && NetworkManager.LocalClientId == OwnerClientId)
		{
			_respawnTimer.Tick(Time.deltaTime);
		}

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
		breadCrumb.GetComponent<BreadCrumb>().InitializeBreadCrumb(CurrentPlayerBiome.Value);
		GameManager.Instance.InvokeSpawnBreadCrumbEvent(breadCrumb);
	}

	private void OnPlayerDamaged(object sender, NetworkHealthState.HitPointsDamagedEventArgs e)
	{
		Debug.Log($"Damaging player {OwnerClientId}");
		GameManager.Instance.PlayDamageNumbers(e.DamageTaken, transform.position, CurrentPlayerBiome.Value);

		OnPlayerDamagedClientRpc(e.DamageTaken, e.SourcePosition, e.KnockbackForce, RpcTarget.Single(OwnerClientId, RpcTargetUse.Persistent));
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void OnPlayerDamagedClientRpc(int damageTaken, Vector3 sourcePosition, float knockbackForce, RpcParams rpcParams = default)
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Applied {damageTaken} Damage to {gameObject.name}!");

		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.PlayerDamaged, transform.position);
		PlayerKnockback.ApplyKnockback(sourcePosition, _knockbackResist, knockbackForce);

		OnDamaged?.Invoke(this, new OnDamagedEventArgs
		{
			DamagerPosition = sourcePosition,
			DamageAmount = damageTaken
		});
	}

	private void OnPlayerDeath(object sender, EventArgs e)
	{
		Debug.Log($"Player {OwnerClientId} is dead!");
		OnPlayerDeathClientRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Persistent));
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void OnPlayerDeathClientRpc(RpcParams rpcParams = default)
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] {gameObject.name} is dead!");

		_respawnTimer.Reset();
		_respawnTimer.OnTimerEnd += RespawnPlayer;

		OnDeath?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
	}

	private void RespawnPlayer(object sender, EventArgs e)
	{
		_respawnTimer.OnTimerEnd -= RespawnPlayer;

		HealthState.HealToFullRpc();
	}

	private void OnPlayerRecovery(object sender, EventArgs e)
	{
		RespawnPlayerClientRpc(RpcTarget.Single(OwnerClientId, RpcTargetUse.Persistent));
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

	public void TogglePvp(bool pvpEnabled)
	{
		PvpEnabled.Value = pvpEnabled;
		Debug.Log($"Pvp enabled: {PvpEnabled.Value}");
	}

	private void HotbarManager_OnSelectedItemUpdated(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		if(IsOwner)
		{
			// NTFS: network variables onvaluechanged is only executed if the value is different
			if(e.SelectedItemIndex == -1)
			{
				SelectedItemIndexNetworkVariable.Value = -1;
			}
			else
			{
				SelectedItemIndexNetworkVariable.Value = e.SelectedItemIndex;
			}
		}
	}
	
	public bool IsHoldingAWand()
	{
		ItemSO mainHandItem = GameManager.Instance.GetItemSOFromItemId(SelectedItemIndexNetworkVariable.Value);
		
		return mainHandItem != null && (mainHandItem is SpellBookItemSO || mainHandItem is WandItemSO);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();

		HealthState.OnHitPointsDamaged -= OnPlayerDamaged;
		HealthState.OnHitPointsDepleted -= OnPlayerDeath;
		HealthState.OnHitPointsReplenished -= OnPlayerRecovery;

		if (IsOwner)
		{
			HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnSelectedItemUpdated;
		}
	}
}
