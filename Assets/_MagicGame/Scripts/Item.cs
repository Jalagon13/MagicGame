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
	[SerializeField] private float _initialCollectDelay = 0.5f;

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
	
	private void Awake()
	{
		_itemGameObject = transform.GetChild(0).gameObject;
		_sr = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
		_rb = GetComponent<Rigidbody2D>();
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

			NetworkManager.NetworkTickSystem.Tick += HandleBiomeVisibility;
		}
		
		UpdateItemDataAndVisuals();
		
		base.OnNetworkSpawn();
	}
	
	private void FixedUpdate()
	{
		if (!_canCollect || _itemCollected || !IsServer) return;

		
		CollectTag closestPlayerCollectTag = null;
		float closestDist = Mathf.Infinity;
		
		foreach (var clientId in NetworkManager.ConnectedClientsIds)
		{
			Player player = NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>();
			float dist = Vector2.Distance(transform.position, player.CollectTag.transform.position);
		
			if(dist < closestDist && dist < _attractRange && player.CurrentBiome.Value == _itemBiome)
			{
				closestPlayerCollectTag = player.CollectTag;
				closestDist = dist;
			}
		}
		
		// Move towards the closest collider if found
		if (closestPlayerCollectTag != null)
		{
			Vector2 currentPosition = _rb.position;
			Vector2 targetPosition = closestPlayerCollectTag.transform.position;
			Vector2 direction = (targetPosition - currentPosition).normalized;
			
			_rb.MovePosition(currentPosition + direction * _attractSpeed * Time.fixedDeltaTime);
			
			// Check if the item is within the bounds of any CollectTag collider
			if(Vector2.Distance(currentPosition, targetPosition) < 0.25f)
			{
				if (/* closestValidCollectCollider.transform.root.GetComponent<Player>().OwnerClientId == NetworkManager.LocalClientId && */ _canCollect && !_itemCollected /* && !InventoryFull() */)
				{
					_itemCollected = true;

					AddItemClientRpc(GameManager.Instance.GetItemIdFromItemSO(_itemInventoryItem.Item), _itemInventoryItem.Quantity, RpcTarget.Single(closestPlayerCollectTag.transform.root.GetComponent<Player>().OwnerClientId, RpcTargetUse.Persistent));
					return;
				}
			}
		}
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void AddItemClientRpc(int itemId, int amount, RpcParams rpcParams = default)
	{
		InventoryManager.Instance.AddItem(GameManager.Instance.GetItemSOFromItemId(itemId), amount);
		GameManager.Instance.DestroyItem(this);
	}
	
	private void OnTriggerStay2D(Collider2D other)
	{
		if(other.TryGetComponent(out CollectTag player))
		{
			// _sr.enabled = false;
		}
	}
	
	public void Initialize(ushort itemId, ushort itemAmount, BiomeType biome)
	{
		_itemId = itemId;
		_itemAmount = itemAmount;
		_itemBiome = biome;
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
		return NetworkManager.ConnectedClients[clientId].PlayerObject.GetComponent<Player>().CurrentBiome.Value == _itemBiome;
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
			if(_itemCollider != null)
			{
				_itemCollider.enabled = true;
			}
		}
				
		NetworkObject.NetworkShow(clientId);
	}

	private void HideItem(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			_itemGameObject.SetActive(false);
			_itemCollider.enabled = false;
		}
				
		NetworkObject.NetworkHide(clientId);
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
			NetworkManager.NetworkTickSystem.Tick -= HandleBiomeVisibility;
		}

		base.OnNetworkDespawn();
	}
}
