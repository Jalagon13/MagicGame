using System;
using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using Unity.Netcode;
using UnityEngine;


namespace ProjectWizard
{
	public class Item : NetworkBehaviour
	{
		[SerializeField] private float _attractRange = 2.75f;
		[SerializeField] private float _attractSpeed = 5f;
		[SerializeField] private float _turnSharpness = 5f;
		[SerializeField] private float _initialCollectDelay = 0.5f;
		[SerializeField] private ZAxisSimulator _zAxisSimulator;

		private NetworkVariable<SyncItemData> _syncItemDataNetworkVariable = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
		private NetworkVariable<float> _zAxisNetworkVariable = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
		public float ZAxis => _zAxisNetworkVariable.Value;
	
		private SpriteRenderer _sr;
		private bool _canCollect, _itemCollected;
		private Rigidbody2D _rb;
		private BiomeType _itemBiome;
		private Collider2D _itemCollider, _wallCollider;
		private GameObject _itemGameObject;
		private Vector2 _velocity;
		private Knockback _knockback;
		private Vector2 _direction;
	
		private void Awake()
		{
			_knockback = new Knockback(null);

			_itemGameObject = transform.GetChild(0).gameObject;
			_sr = transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>();
			_rb = GetComponent<Rigidbody2D>();
			_wallCollider = transform.GetChild(0).GetChild(2).GetComponent<Collider2D>();
		}
	
		private IEnumerator Start()
		{
			yield return new WaitForSeconds(_initialCollectDelay);
			_canCollect = true;
		}

		public override void OnNetworkSpawn()
		{
			if(IsClient)
			{
				UpdateItemDataAndVisuals();
			}
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
		
				if(dist < closestDist && dist < _attractRange && player.CurrentBiome.Value == _itemBiome)
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
				_wallCollider.enabled = false;

				// Check if the item is within the bounds of any CollectTag collider
				if (Vector2.Distance(currentPosition, targetPosition) < 0.25f)
				{
					if (/* closestValidCollectCollider.transform.root.GetComponent<Player>().OwnerClientId == NetworkManager.LocalClientId && */ _canCollect && !_itemCollected /* && !InventoryFull() */)
					{
						_itemCollected = true;

						AddItemClientRpc(_syncItemDataNetworkVariable.Value, RpcTarget.Single(closestPlayerCollectTag.transform.root.GetComponent<Player>().OwnerClientId, RpcTargetUse.Persistent));
						return;
					}
				}
			}
			else
			{
				_wallCollider.enabled = true;
			}

			_knockback.UpdateKnockback(Time.fixedDeltaTime);

			if (closestPlayerCollectTag == null)
			{
				_velocity = Vector2.Lerp(_velocity, Vector2.zero, _turnSharpness * Time.fixedDeltaTime);
			}
			else
			{
				if (_knockback.KnockbackActive)
				{
					_velocity = _direction + _knockback.Velocity;
				}
			}

			_rb.linearVelocity = _velocity;
		}

		public void Initialize(SyncItemData syncItemData, BiomeType biome, Vector2 velocity, float startingZAxis)
		{
			_syncItemDataNetworkVariable.Value = syncItemData;
			_zAxisNetworkVariable.Value = startingZAxis;
			_itemBiome = biome;
			_velocity = velocity;
			_itemCollider = GetComponent<Collider2D>();

			HideItem(NetworkManager.ServerClientId);

			if (_velocity != Vector2.zero)
			{
				_knockback.ApplyKnockbackCustomDirection(_velocity, 0);
			}

			NetworkObject.CheckObjectVisibility += CheckIfInSameEnvironment;
			NetworkManager.NetworkTickSystem.Tick += HandleBiomeVisibility;

			UpdateItemDataAndVisuals();
		}

		[Rpc(SendTo.SpecifiedInParams)]
		private void AddItemClientRpc(SyncItemData syncItemData, RpcParams rpcParams = default)
		{
			ItemDataSO itemSO = GameDataRegistry.Instance.GetItemDataFromItemId(syncItemData.ItemId);
		
			InventoryItem inventoryItem = new();
		
			if(itemSO is WandItemSO wandItemSO)
			{
				WandInventoryItem wandInventoryItem = new(itemSO, syncItemData.Quantity, wandItemSO.Capacity, syncItemData.SelectedSpellIndex);
			
				for (int i = 0; i < wandInventoryItem.MagicArray.Length; i++)
				{
					wandInventoryItem.SetMagic(GameDataRegistry.Instance.GetItemDataFromItemId(syncItemData.MagicArray[i]) as SpellItemSO, i);
				}
			
				inventoryItem = wandInventoryItem;
			}
			else
			{
				inventoryItem = new InventoryItem(itemSO, syncItemData.Quantity);
			}
		
			InventoryManager.Instance.AddItem(inventoryItem);
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
			ItemDataSO itemSO = GameDataRegistry.Instance.GetItemDataFromItemId(_syncItemDataNetworkVariable.Value.ItemId);
		
			_sr.sprite = itemSO.UiDisplay;
			_zAxisSimulator.SetZAxis(_zAxisNetworkVariable.Value);
			gameObject.name = $"Item_{itemSO.InGameName}";
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

}