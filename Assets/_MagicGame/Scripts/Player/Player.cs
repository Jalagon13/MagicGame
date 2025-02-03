using System;
using System.Collections;
using System.Collections.Generic;
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
	
	[field: SerializeField] public PlayerHand MainHand { get; private set; }
	[field: SerializeField] public PlayerHand OffHand { get; private set; }
	[SerializeField] private float _respawnTimerDuration;
	[SerializeField] private List<InventoryItem> _startingItems = new();
	
	public PlayerStats PlayerStats { get; private set; }
	public NetworkVariable<int> MainHandItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<int> OffHandItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<BiomeType> CurrentBiome { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public Collider2D HitCollider { get; private set; }
	public bool IsPerformingSwing { get; set; }
	
	private NetworkVariable<int> _healthNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Knockback _knockback;
	private Rigidbody2D _rb;
	private Timer _respawnTimer;
	private Vector2 _spawnPoint;
	private BiomeType _spawnBiome;
	
	
	private void Awake()
	{
		PlayerStats = GetComponent<PlayerStats>();
		HitCollider = GetComponent<Collider2D>();
		_knockback = GetComponent<Knockback>();
		_rb = GetComponent<Rigidbody2D>();
		_respawnTimer = new(_respawnTimerDuration);
		_healthNetworkVariable.OnValueChanged += HealthNetworkVariable_OnValueChanged;
	}
	
	public override void OnNetworkSpawn()
	{
		gameObject.name = $"Player_{OwnerClientId}";
		
		if(IsOwner)
		{
			LocalClientInstance = this;
			
			CurrentBiome.Value = BiomeType.Forest; // For now all players will spawn in the forest
			_spawnBiome = BiomeType.Forest;
			_spawnPoint = transform.position;
			
			HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnMainHandSlotUpdated;
			InventoryManager.Instance.OnOffHandItemUpdated += InventoryManager_OnOffHandItemUpdated;
			
			Invoke(nameof(SpawnStartingItems), 0.25f);
		}
		
		OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
		
		if(IsServer)
		{
			_healthNetworkVariable.Value = PlayerStats.StartingPlayerHealth;
		}
	}

	private void Update()
	{
		if(IsDead() && NetworkManager.LocalClientId == OwnerClientId)
		{
			_respawnTimer.Tick(Time.deltaTime);
		}
	}
	
	private void SpawnStartingItems()
	{
		foreach (InventoryItem item in _startingItems)
		{
			InventoryManager.Instance.AddItem(item.Item, item.Quantity);
		}
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
	
	#region Damage Functions
	
	public void ApplyDamage(int damage, Vector2 damagerPosition)
	{
		if (IsDead()) return;
		
		// Apply the final damage to the player
		ApplyPlayerDamageServerRpc(OwnerClientId, damage, damagerPosition);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void ApplyPlayerDamageServerRpc(ulong damagePlayerId, int damageAmount, Vector2 damagerPosition)
	{
		if (!NetworkManager.ConnectedClients.ContainsKey(damagePlayerId))
		{
			Debug.LogWarning($"Invalid damagePlayerId: {damagePlayerId}");
			return;
		}
	
		int defense = NetworkManager.ConnectedClients[damagePlayerId].PlayerObject.GetComponent<Player>().PlayerStats.PlayerDefense;
		int damageReduction = defense / 2; // Defense reduces damage by half its value
		
		// Apply damage reduction
		int finalDamage = Mathf.Max(1, damageAmount - damageReduction); // Minimum damage is 1
	
		_healthNetworkVariable.Value = Mathf.Max(0, _healthNetworkVariable.Value - finalDamage);
		
		bool isPlayerDead = _healthNetworkVariable.Value <= 0;
		
		OnPlayerHealthChangedClientRpc(finalDamage, isPlayerDead, damagerPosition, damagePlayerId);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void OnPlayerHealthChangedClientRpc(int finalDamage, bool isKilled, Vector2 damagerPosition, ulong damagePlayerId)
	{
		if(OwnerClientId != damagePlayerId) return;
		
		Debug.Log($"[Client {NetworkManager.LocalClientId}] {gameObject.name} health changed");
		
		if(isKilled)
		{
			OnPlayerKilled();
		}
		else
		{
			OnPlayerDamaged(finalDamage, damagerPosition);
		}
	}
	
	private void OnPlayerKilled()
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] {gameObject.name} is dead!");
		
		if(NetworkManager.LocalClientId == OwnerClientId)
		{
			_respawnTimer.Reset();
			_respawnTimer.OnTimerEnd += RespawnPlayer;
		}
		
		OnDeath?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
	}
	
	private void OnPlayerDamaged(int finalDamage, Vector2 damagerPosition)
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Applied {finalDamage} Damage to {gameObject.name}!");
		
		_knockback?.ApplyKnockback(_rb, damagerPosition);
		
		OnDamaged?.Invoke(this, new OnDamagedEventArgs
		{
			DamagerPosition = damagerPosition,
			DamageAmount = finalDamage
		});
	}
	
	#endregion
	
	private void RespawnPlayer(object sender, EventArgs e)
	{
		_respawnTimer.OnTimerEnd -= RespawnPlayer;
		
		RespawnPlayerServerRpc(OwnerClientId, PlayerStats.StartingPlayerHealth);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void RespawnPlayerServerRpc(ulong respawnerId, int healthToRespawnWith)
	{
		_healthNetworkVariable.Value = healthToRespawnWith;
		
		RespawnPlayerClientRpc(RpcTarget.Single(respawnerId, RpcTargetUse.Persistent));
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void RespawnPlayerClientRpc(RpcParams rpcParams = default)
	{
		if(NetworkManager.LocalClientId == rpcParams.Receive.SenderClientId)
		{
			transform.SetPositionAndRotation(_spawnPoint, Quaternion.identity);
		}
		
		if(LocalClientInstance.CurrentBiome.Value != _spawnBiome)
		{
			WorldManager.Instance.LoadEnvironment(_spawnBiome, _spawnPoint, isPlayerRespawning: true);
		}

		OnRespawn?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
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
	
	 private void InventoryManager_OnOffHandItemUpdated(object sender, InventoryManager.InventoryItemEventArgs e)
	{
		if(IsOwner)
		{
			// NTFS: network variables onvaluechanged is only executed if the value is different
			var offHandItemIndex = GameManager.Instance.GetItemIdFromItemSO(e.InventoryItem.Item);
			
			if(offHandItemIndex == -1)
			{
				OffHandItemIndexNetworkVariable.Value = -1;
			}
			else
			{
				OffHandItemIndexNetworkVariable.Value = offHandItemIndex;
			}
		}
	}
	
	public bool IsDead()
	{
		return _healthNetworkVariable.Value <= 0;
	}
	
	public bool IsHoldingAWand()
	{
		ItemSO mainHandItem = GameManager.Instance.GetItemSOFromItemId(MainHandItemIndexNetworkVariable.Value);
		bool mainHandHoldingWand = mainHandItem != null && mainHandItem is WandItemSO;
		
		ItemSO offHandItem = GameManager.Instance.GetItemSOFromItemId(OffHandItemIndexNetworkVariable.Value);
		bool offHandHoldingWand = offHandItem != null && offHandItem is WandItemSO;
		
		return mainHandHoldingWand || offHandHoldingWand;
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		
		_healthNetworkVariable.OnValueChanged -= HealthNetworkVariable_OnValueChanged;
		
		if(IsOwner)
		{
			HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnMainHandSlotUpdated;
			InventoryManager.Instance.OnOffHandItemUpdated -= InventoryManager_OnOffHandItemUpdated;
		}
	}
}
