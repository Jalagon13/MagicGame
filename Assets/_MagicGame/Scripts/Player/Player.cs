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
	public PlayerStats PlayerStats { get; private set; }
	public NetworkVariable<int> MainHandItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<int> OffHandItemIndexNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<BiomeType> CurrentBiome { get; set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public BiomeType Biome { get { return CurrentBiome.Value; } }
	public Collider2D HitCollider { get; private set; }
	public bool IsPerformingSwing { get; set; }
	
	[SerializeField] private float _respawnTimerDuration;
	[field: SerializeField] public float IFrameLength { get; private set; } = 0.67f;
	[Range(0, 100), SerializeField] private float _knockbackResist;
	[SerializeField] private List<InventoryItem> _startingItems = new();

	public NetworkVariable<int> HealthNetworkVariable { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	public NetworkVariable<bool> ExecutingIFrames { get; private set; } = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
	
	public Knockback _playerKnockback { get; private set; }
	private Timer _respawnTimer;
	private Vector2 _spawnPoint;
	private BiomeType _spawnBiome;
	
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
			
			CurrentBiome.Value = BiomeType.Forest; // For now all players will spawn in the forest
			_spawnBiome = BiomeType.Forest;
			_spawnPoint = transform.position;
			
			HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnMainHandSlotUpdated;
			InventoryManager.Instance.OnOffHandItemUpdated += InventoryManager_OnOffHandItemUpdated;
			GameInput.Instance.OnSpaceStarted += DashTest;
			
			StartCoroutine(SpawnStartingItems());
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

	private void DashTest(object sender, EventArgs e)
	{
		if(Pointer.IsOverUI() || ObjectManager.Instance.TryToFindWorldObject(Vector2Int.FloorToInt(ActionManager.MouseWorldPosition), out WorldObject wo)) return;
		_playerKnockback.ApplyKnockback(ActionManager.MouseWorldPosition, _knockbackResist, 30, true); 
	}

	private void Update()
	{
		if(IsDead() && NetworkManager.LocalClientId == OwnerClientId)
		{
			_respawnTimer.Tick(Time.deltaTime);
		}
	}
	
	private IEnumerator SpawnStartingItems()
	{
		yield return null;
		
		foreach (InventoryItem item in _startingItems)
		{
			InventoryManager.Instance.AddItem(item.Item, item.Quantity);
			yield return null;
		}
	}
	
	#region Damage Functions
	
	public void ApplyDamage(int damage, Vector2 damagerPosition, int knockbackForce)
	{
		if (IsDead()) return;
		Debug.Log($"Damaging {gameObject.name} from damager pos {damagerPosition}, with knockback {knockbackForce}");
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
		
		GameManager.Instance.PlayDamageNumbers(damageAmount, transform.position, CurrentBiome.Value);
		
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
		Debug.Log($"[Client {NetworkManager.LocalClientId}] {gameObject.name} health changed");
		
		if(isKilled)
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
		else
		{
			Debug.Log($"[Client {NetworkManager.LocalClientId}] Applied {finalDamage} Damage to {gameObject.name}!");
			
			_playerKnockback.ApplyKnockback(damagerPosition, _knockbackResist, knockbackForce); 
			
			OnDamaged?.Invoke(this, new OnDamagedEventArgs
			{
				DamagerPosition = damagerPosition,
				DamageAmount = finalDamage
			});
		}
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
		HealthNetworkVariable.Value = healthToRespawnWith;
		
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
			WorldManager.Instance.LoadBiome(_spawnBiome, _spawnPoint, false);
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
		return HealthNetworkVariable.Value <= 0;
	}
	
	public bool IsHoldingAWand()
	{
		ItemSO mainHandItem = GameManager.Instance.GetItemSOFromItemId(MainHandItemIndexNetworkVariable.Value);
		bool mainHandHoldingWand = mainHandItem != null && mainHandItem is SpellBookItemSO;
		
		ItemSO offHandItem = GameManager.Instance.GetItemSOFromItemId(OffHandItemIndexNetworkVariable.Value);
		bool offHandHoldingWand = offHandItem != null && offHandItem is SpellBookItemSO;
		
		return mainHandHoldingWand || offHandHoldingWand;
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		
		HealthNetworkVariable.OnValueChanged -= HealthNetworkVariable_OnValueChanged;
		
		if(IsOwner)
		{
			HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnMainHandSlotUpdated;
			InventoryManager.Instance.OnOffHandItemUpdated -= InventoryManager_OnOffHandItemUpdated;
		}
	}
}
