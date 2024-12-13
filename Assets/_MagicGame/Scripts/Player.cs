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
	public event EventHandler<OnStatUpdatedEventArgs> OnPlayerManaUpdated;
	public class OnStatUpdatedEventArgs : EventArgs
	{
		public int PreviousValue;
		public int NewValue;
		public int MaxValue;
	}
	
	[SerializeField] private int _startingMana = 100;
	[SerializeField] private int _startingHealth = 100;
	[SerializeField] private float _respawnTimerDuration;
	[SerializeField] private Transform _wandProjectileSpawnPoint;
	[SerializeField] private List<InventoryItem> _startingItems = new();
	
	private NetworkVariable<int> _focusItemIndexNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private NetworkVariable<int> _healthNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private NetworkVariable<int> _manaNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private Knockback _knockback;
	private Rigidbody2D _rb;
	private Timer _respawnTimer;
	private Vector2 _spawnPoint;
	private bool _isSwingGoingOn;
	
	private void Awake()
	{
		_knockback = GetComponent<Knockback>();
		_rb = GetComponent<Rigidbody2D>();
		_respawnTimer = new(_respawnTimerDuration);
		_healthNetworkVariable.OnValueChanged += HealthNetworkVariable_OnValueChanged;
		_manaNetworkVariable.OnValueChanged += ManaNetworkVariable_OnValueChanged;
		_spawnPoint = transform.position;
	}
	
	public override void OnNetworkSpawn()
	{
		if(IsOwner)
		{
			LocalClientInstance = this;
			
			HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnFocusSlotUpdated;
			
			Invoke(nameof(SpawnStartingItems), 0.25f);
		}
		
		if(IsServer)
		{
			_healthNetworkVariable.Value = _startingHealth;
			_manaNetworkVariable.Value = _startingMana;
		}
		
		OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
		
		RefreshUI();
		
		gameObject.name = $"Player_{OwnerClientId}";
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
		
		RespawnPlayerClientRpc(respawnerId);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void RespawnPlayerClientRpc(ulong respawnerId)
	{
		// If this code is running on the client who called respawn, then execute respawn logic
		if(NetworkManager.LocalClientId == respawnerId)
		{
			transform.SetPositionAndRotation(_spawnPoint, Quaternion.identity);
		}

		OnRespawn?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
	}
	
	private void ManaNetworkVariable_OnValueChanged(int previousValue, int newValue)
	{
		OnPlayerManaUpdated?.Invoke(this, new OnStatUpdatedEventArgs
		{
			PreviousValue = previousValue,
			NewValue = newValue,
			MaxValue = _startingMana
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

	private void HotbarManager_OnFocusSlotUpdated(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		if(IsOwner)
		{
			if(HotbarManager.Instance.GetFocusInventoryItem() != null)
			{
				_focusItemIndexNetworkVariable.Value = e.FocusItemIndex;
			}
			else
			{
				_focusItemIndexNetworkVariable.Value = -1;
			}
		}
	}
	
	private void SpawnStartingItems()
	{
		foreach (InventoryItem item in _startingItems)
		{
			InventoryManager.Instance.AddItem(item);
		}
		
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}

	public Transform GetWandProjectileSpawnPoint()
	{
		return _wandProjectileSpawnPoint;
	}
	
	public NetworkVariable<int> GetFocusItemIndexNetworkVariable()
	{
		return _focusItemIndexNetworkVariable;
	}
	
	public void RefreshUI()
	{
		HealthNetworkVariable_OnValueChanged(_healthNetworkVariable.Value, _healthNetworkVariable.Value);
		ManaNetworkVariable_OnValueChanged(_manaNetworkVariable.Value, _manaNetworkVariable.Value);
	}
	
	public bool IsDead()
	{
		return _healthNetworkVariable.Value <= 0;
	}

	public bool IsSwingGoingOn()
	{
		return _isSwingGoingOn;
	}
	
	public void SetIsSwingOnGoingOn(bool _)
	{
		_isSwingGoingOn = _;
	}
	
	public bool IsHoldingWand()
	{
		ItemSO focusItem = GameManager.Instance.GetItemSOFromIndex(_focusItemIndexNetworkVariable.Value);
		
		if(focusItem == null)
		{
			return false;
		}
		
		
		return focusItem is WandItemSO;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		
		_healthNetworkVariable.OnValueChanged -= HealthNetworkVariable_OnValueChanged;
		_manaNetworkVariable.OnValueChanged -= ManaNetworkVariable_OnValueChanged;
		
		if(IsOwner)
		{
			HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnFocusSlotUpdated;
		}
	}
}
