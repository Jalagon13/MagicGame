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
	
	public event EventHandler<IHasHealth.OnHealthUpdatedEventArgs> OnPlayerHealthUpdated;
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
	
	private NetworkVariable<EnvironmentID> _playerEnvironment = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private NetworkVariable<int> _mainHandItemIndexNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private NetworkVariable<int> _offHandItemIndexNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private NetworkVariable<int> _healthNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	private NetworkVariable<int> _manaNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private Knockback _knockback;
	private Rigidbody2D _rb;
	private Timer _respawnTimer;
	private Vector2 _spawnPoint;
	private EnvironmentID _spawnEnvironment;
	private bool _isPerformingSwing;
	
	private void Awake()
	{
		_knockback = GetComponent<Knockback>();
		_rb = GetComponent<Rigidbody2D>();
		_respawnTimer = new(_respawnTimerDuration);
		_healthNetworkVariable.OnValueChanged += HealthNetworkVariable_OnValueChanged;
		_manaNetworkVariable.OnValueChanged += ManaNetworkVariable_OnValueChanged;
	}
	
	public override void OnNetworkSpawn()
	{
		gameObject.name = $"Player_{OwnerClientId}";
		
		if(IsOwner)
		{
			LocalClientInstance = this;
			
			_playerEnvironment.Value = EnvironmentID.Forest; // For now all players will spawn in the forest
			_spawnEnvironment = EnvironmentID.Forest;
			_spawnPoint = transform.position;
			
			HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnMainHandSlotUpdated;
			InventoryManager.Instance.OnOffHandItemUpdated += InventoryManager_OnOffHandItemUpdated;
			
			Invoke(nameof(SpawnStartingItems), 0.25f);
			InvokeRepeating(nameof(ManaRegen), 1f, 1f);
		}
		
		OnAnyPlayerSpawned?.Invoke(this, new PlayerIdEventArgs
		{
			PlayerId = OwnerClientId
		});
		
		if(IsServer)
		{
			_healthNetworkVariable.Value = _startingHealth;
		}
		
		if(IsOwner)
		{
			_manaNetworkVariable.Value = _startingMana;
		}
	}

	private void ManaRegen()
	{
		if(_manaNetworkVariable.Value < _startingMana)
		{
			_manaNetworkVariable.Value += 1;
		}
	}

	private void Update()
	{
		if(IsDead() && NetworkManager.LocalClientId == OwnerClientId)
		{
			_respawnTimer.Tick(Time.deltaTime);
		}
	}
	
	public void RemoveMana(int amount)
	{
		_manaNetworkVariable.Value -= amount;
		
		if(_manaNetworkVariable.Value < 0)
		{
			_manaNetworkVariable.Value = 0;
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
		
		if(Player.LocalClientInstance.GetPlayerEnvironment() != _spawnEnvironment)
		{
			WorldManager.Instance.LoadEnvironment(_spawnEnvironment, _spawnPoint, isPlayerRespawning: true);
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
		OnPlayerHealthUpdated?.Invoke(this, new IHasHealth.OnHealthUpdatedEventArgs
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
				_mainHandItemIndexNetworkVariable.Value = -1;
			}
			else
			{
				_mainHandItemIndexNetworkVariable.Value = e.MainHandItemIndex;
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
				_offHandItemIndexNetworkVariable.Value = -1;
			}
			else
			{
				_offHandItemIndexNetworkVariable.Value = offHandItemIndex;
			}
		}
	}
	
	private void SpawnStartingItems()
	{
		foreach (InventoryItem item in _startingItems)
		{
			InventoryManager.Instance.AddItem(item.Item, item.Quantity);
		}
		Debug.Log("Start Items spawned");
		InventoryManager.Instance.GetInventoryModel().UpdateInventory();
	}

	public Transform GetWandProjectileSpawnPoint()
	{
		return _wandProjectileSpawnPoint;
	}
	
	public NetworkVariable<int> GetMainHandItemIndexNetworkVariable()
	{
		return _mainHandItemIndexNetworkVariable;
	}
	
	public NetworkVariable<int> GetOffHandItemIndexNetworkVariable()
	{
		return _offHandItemIndexNetworkVariable;
	}
	
	public bool IsDead()
	{
		return _healthNetworkVariable.Value <= 0;
	}

	public bool GetIsPerformingSwing()
	{
		return _isPerformingSwing;
	}
	
	public void SetIsPerformingSwing(bool _)
	{
		_isPerformingSwing = _;
	}
	
	public bool IsHoldingAWand()
	{
		ItemSO mainHandItem = GameManager.Instance.GetItemSOFromIndex(_mainHandItemIndexNetworkVariable.Value);
		bool mainHandHoldingWand = mainHandItem != null && mainHandItem is WandItemSO;
		
		ItemSO offHandItem = GameManager.Instance.GetItemSOFromIndex(_offHandItemIndexNetworkVariable.Value);
		bool offHandHoldingWand = offHandItem != null && offHandItem is WandItemSO;
		
		bool isHoldingAWand = (mainHandHoldingWand || offHandHoldingWand) && !_isPerformingSwing;
		
		return isHoldingAWand;
	}
	
	public EnvironmentID GetPlayerEnvironment()
	{
		return _playerEnvironment.Value;
	}
	
	public void SetPlayerEnvironment(EnvironmentID environment)
	{
		Debug.Log($"Setting player {OwnerClientId} environement to: {environment}");
		_playerEnvironment.Value = environment;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		
		_healthNetworkVariable.OnValueChanged -= HealthNetworkVariable_OnValueChanged;
		_manaNetworkVariable.OnValueChanged -= ManaNetworkVariable_OnValueChanged;
		
		if(IsOwner)
		{
			HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnMainHandSlotUpdated;
			InventoryManager.Instance.OnOffHandItemUpdated -= InventoryManager_OnOffHandItemUpdated;
		}
	}
}
