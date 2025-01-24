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
	[SerializeField] private int _startingHealth = 100;
	[SerializeField] private float _respawnTimerDuration;
	[SerializeField] private List<InventoryItem> _startingItems = new();
	public NetworkVariable<int> MainHandItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<int> OffHandItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<EnvironmentID> PlayerEnvironment { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public Collider2D HitCollider { get; private set; }
	public bool IsPerformingSwing { get; set; }
	
	private NetworkVariable<int> _healthNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Knockback _knockback;
	private Rigidbody2D _rb;
	private Timer _respawnTimer;
	private Vector2 _spawnPoint;
	private EnvironmentID _spawnEnvironment;
	
	
	private void Awake()
	{
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
			
			PlayerEnvironment.Value = EnvironmentID.Forest; // For now all players will spawn in the forest
			_spawnEnvironment = EnvironmentID.Forest;
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
			_healthNetworkVariable.Value = _startingHealth;
		}
	}

	private void Update()
	{
		if(IsDead() && NetworkManager.LocalClientId == OwnerClientId)
		{
			_respawnTimer.Tick(Time.deltaTime);
		}
	}
	
	public void ApplyDamage(int damage, Vector2 damagerPosition)
	{
		if(IsDead()) return;
	
		ApplyPlayerDamageServerRpc(OwnerClientId, damage, damagerPosition);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void ApplyPlayerDamageServerRpc(ulong damagePlayerId, int damageAmount, Vector2 damagerPosition)
	{
		_healthNetworkVariable.Value -= damageAmount;
		
		bool isPlayerDead = _healthNetworkVariable.Value <= 0;
		
		OnPlayerHealthChangedClientRpc(damageAmount, isPlayerDead, damagerPosition, damagePlayerId);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void OnPlayerHealthChangedClientRpc(int damageAmount, bool isKilled, Vector2 damagerPosition, ulong damagePlayerId)
	{
		if(OwnerClientId != damagePlayerId) return;
		
		Debug.Log($"[Client {NetworkManager.LocalClientId}] {gameObject.name} health changed");
		
		if(isKilled)
		{
			OnPlayerKilled();
		}
		else
		{
			OnPlayerDamaged(damageAmount, damagerPosition);
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
	
	private void OnPlayerDamaged(int damageAmount, Vector2 damagerPosition)
	{
		Debug.Log($"[Client {NetworkManager.LocalClientId}] Applied {damageAmount} Damage to {gameObject.name}!");
		
		_knockback?.ApplyKnockback(_rb, damagerPosition);
		
		OnDamaged?.Invoke(this, new OnDamagedEventArgs
		{
			DamagerPosition = damagerPosition,
			DamageAmount = damageAmount
		});
	}
	
	private void RespawnPlayer(object sender, EventArgs e)
	{
		_respawnTimer.OnTimerEnd -= RespawnPlayer;
		
		RespawnPlayerServerRpc(OwnerClientId, _startingHealth);
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
		
		if(LocalClientInstance.PlayerEnvironment.Value != _spawnEnvironment)
		{
			WorldManager.Instance.LoadEnvironment(_spawnEnvironment, _spawnPoint, isPlayerRespawning: true);
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
			MaxValue = _startingHealth
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
			var offHandItemIndex = GameManager.Instance.GetItemIndexFromItemSO(e.InventoryItem.Item);
			
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
	
	private void SpawnStartingItems()
	{
		foreach (InventoryItem item in _startingItems)
		{
			InventoryManager.Instance.AddItem(item.Item, item.Quantity);
		}
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}
	
	public bool IsDead()
	{
		return _healthNetworkVariable.Value <= 0;
	}
	
	public bool IsHoldingAWand()
	{
		ItemSO mainHandItem = GameManager.Instance.GetItemSOFromIndex(MainHandItemIndexNetworkVariable.Value);
		bool mainHandHoldingWand = mainHandItem != null && mainHandItem is WandItemSO;
		
		ItemSO offHandItem = GameManager.Instance.GetItemSOFromIndex(OffHandItemIndexNetworkVariable.Value);
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
