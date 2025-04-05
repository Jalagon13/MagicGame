using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
{
	[SerializeField] private float _attractRange = 2.75f;
	[SerializeField] private float _attractSpeed = 5f;
	[SerializeField] private float _turnSharpness = 5f;
	[SerializeField] private float _initialCollectDelay = 0.5f;
	[SerializeField] private float _dropForce = 3f;

	private NetworkVariable<int> _itemIdNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private NetworkVariable<int> _itemAmountNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private InventoryItem _itemInventoryItem;
	private SpriteRenderer _sr;
	private bool _canCollect, _itemCollected;
	private Rigidbody2D _rb;
	private ushort _itemId, _itemAmount;
	private BiomeType _itemBiome;
	private Collider2D _itemCollider;
	private GameObject _itemGameObject;
	private Vector2 _velocity;
	private Knockback _knockback;
	private Vector2 _direction;
	
	private void Awake()
	{
		_itemGameObject = transform.GetChild(0).gameObject;
		_sr = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
		_rb = GetComponent<Rigidbody2D>();
		_knockback = GetComponent<Knockback>();
	}
	
	private IEnumerator Start()
	{
		yield return new WaitForSeconds(_initialCollectDelay);
		_canCollect = true;
	}

	public override void OnNetworkSpawn()
	{
		if(IsServer)
		{
			_itemIdNetworkVariable.Value = _itemId;
			_itemAmountNetworkVariable.Value = _itemAmount;
			_itemCollider = GetComponent<Collider2D>();
			
			HideItem(NetworkManager.ServerClientId);
			
			if(_velocity != Vector2.zero)
			{
				Debug.Log($"item Velocity: {_velocity}");
				_knockback.ApplyKnockbackCustomDirection(_velocity, 0, _velocity.magnitude);
			}

			NetworkObject.CheckObjectVisibility += CheckIfInSameEnvironment;
			NetworkManager.NetworkTickSystem.Tick += HandleBiomeVisibility;
		}
		
		UpdateItemDataAndVisuals();
		
		base.OnNetworkSpawn();
	}
	
	private void FixedUpdate()
	{
		if (_itemCollected || !IsServer) return;
		
		CollectTag closestPlayerCollectTag = null;
		float closestDist = Mathf.Infinity;
		
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			Player player = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
			float dist = Vector2.Distance(transform.position, player.CollectTag.transform.position);
		
			if(dist < closestDist && dist < _attractRange && player.CurrentPlayerBiome.Value == _itemBiome)
			{
				closestPlayerCollectTag = player.CollectTag;
				closestDist = dist;
			}
		}

		if (closestPlayerCollectTag != null && _canCollect)
		{
			Vector2 currentPosition = _rb.position;
			Vector2 targetPosition = closestPlayerCollectTag.transform.position;
			_direction = (targetPosition - currentPosition).normalized;
			_velocity = Vector2.Lerp(_velocity, _direction * _attractSpeed, _turnSharpness * Time.fixedDeltaTime);
			_rb.linearVelocity = _velocity;

			// Check if the item is within the bounds of any CollectTag collider
			if (Vector2.Distance(currentPosition, targetPosition) < 0.25f)
			{
				if (/* closestValidCollectCollider.transform.root.GetComponent<Player>().OwnerClientId == NetworkManager.LocalClientId && */ _canCollect && !_itemCollected /* && !InventoryFull() */)
				{
					_itemCollected = true;

					AddItemClientRpc(GameManager.Instance.GetItemIdFromItemSO(_itemInventoryItem.Item), _itemInventoryItem.Quantity, RpcTarget.Single(closestPlayerCollectTag.transform.root.GetComponent<Player>().OwnerClientId, RpcTargetUse.Persistent));
					return;
				}
			}
		}

		if (_knockback.KnockbackActive)
		{
			_velocity = _direction + _knockback.Velocity;
		}
		
		if(!_knockback.KnockbackActive && closestPlayerCollectTag == null)
		{
			_velocity = Vector2.zero;
		}

		_rb.linearVelocity = _velocity;
	}

	public void Initialize(ushort itemId, ushort itemAmount, BiomeType biome, Vector2 velocity)
	{
		_itemId = itemId;
		_itemAmount = itemAmount;
		_itemBiome = biome;
		_velocity = velocity * _dropForce;
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void AddItemClientRpc(int itemId, int amount, RpcParams rpcParams = default)
	{
		InventoryManager.Instance.AddItem(GameManager.Instance.GetItemSOFromItemId(itemId), amount);
		GameManager.Instance.DestroyItem(this);
	}
	
	private void HandleBiomeVisibility()
	{
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			var isInSameEnvironment = CheckIfInSameEnvironment(clientId);
			var isVisibile = NetworkObjectVisibleTo(clientId);
			
			if(isInSameEnvironment && !isVisibile)
			{
				ShowItem(clientId);
			}
			else if(!isInSameEnvironment && isVisibile)
			{
				HideItem(clientId);
			}
		}
	}

	private bool CheckIfInSameEnvironment(ulong clientId)
	{
		return NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().CurrentPlayerBiome.Value == _itemBiome;
	}

	private bool NetworkObjectVisibleTo(ulong clientId)
	{
		return clientId == NetworkManager.ServerClientId ? _itemGameObject.activeInHierarchy : NetworkObject.IsNetworkVisibleTo(clientId);
	}

	private void ShowItem(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_itemGameObject.SetActive(true);
			_itemCollider.enabled = true;
		}
		else
		{
			NetworkObject.NetworkShow(clientId);
		}
	}

	private void HideItem(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_itemGameObject.SetActive(false);
			_itemCollider.enabled = false;
		}
		else
		{
			NetworkObject.NetworkHide(clientId);
		}
	}

	private void UpdateItemDataAndVisuals()
	{
		ItemSO itemSO = GameManager.Instance.GetItemSOFromItemId(_itemIdNetworkVariable.Value);
		
		_itemInventoryItem = new(itemSO, _itemAmountNetworkVariable.Value);
		_sr.sprite = _itemInventoryItem.Item.UiDisplay;
		gameObject.name = $"Item_{_itemInventoryItem.Item.Name}";
	}
	
	public void DestroySelf()
	{
		Destroy(gameObject);
	}
	
	public override void OnNetworkDespawn()
	{
		if (IsServer)
		{
			NetworkObject.CheckObjectVisibility -= CheckIfInSameEnvironment;
			NetworkManager.NetworkTickSystem.Tick -= HandleBiomeVisibility;
		}

		base.OnNetworkDespawn();
	}
}
