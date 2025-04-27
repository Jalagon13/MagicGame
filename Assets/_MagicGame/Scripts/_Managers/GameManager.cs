using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class GameManager : NetworkBehaviour
{
	public static GameManager Instance { get; private set; }

	public event EventHandler<BreadCrumbEventArgs> OnSpawnBreadCrumbPrefab;
	public class BreadCrumbEventArgs : EventArgs
	{
		public GameObject SpawnedBreadCrumbPrefab;
	}

	[SerializeField] private BiomeType _startingBiome;
	
	[Title("Item Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private GameObject _itemBasePrefab;
	[SerializeField] private GameObject _playerPrefab;
	
	[Title("Database Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private ItemDataBaseSO _itemDataBaseSO;
	[SerializeField] private WorldObjectDataBaseSO _worldObjectDataBaseSO;
	[SerializeField] private TileDataBaseSO _tileDataBaseSO;
	[field: SerializeField] public NpcDataBaseSO NpcDataBaseSO { get; private set; }
	
	private void Awake()
	{
		Instance = this;
	}
	
	#region Scene Functions
	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	// This function is called whenever a scene has finished loading
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if(NetworkManager.Singleton == null) return;

		// Prevent duplicate subscriptions
		NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
		NetworkManager.Singleton.OnClientConnectedCallback -= Pathfinding_OnClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback -= Pathfinding_OnClientDisconnected;

		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
		NetworkManager.Singleton.OnClientConnectedCallback += Pathfinding_OnClientConnected;
		NetworkManager.Singleton.OnClientDisconnectCallback += Pathfinding_OnClientDisconnected;
		
		if(Loader.IsHost)
		{
			NetworkManager.Singleton.StartHost();
		}
		else
		{
			NetworkManager.Singleton.StartClient();
		}
	}

	private void Pathfinding_OnClientConnected(ulong obj)
	{
		Pathfinding.Instance.OnClientConnected(obj);
	}

	private void Pathfinding_OnClientDisconnected(ulong obj)
	{
		Pathfinding.Instance.OnClientDisconnected(obj);
	}

	private void OnClientConnected(ulong clientId)
	{
		if(NetworkManager.LocalClientId != clientId) return;
	
		LoadBiomeClientRpc(_startingBiome, RpcTarget.Single(clientId, RpcTargetUse.Persistent));
	}
	
	[Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
	private void LoadBiomeClientRpc(BiomeType biome, RpcParams rpcParams = default)
	{
		if(NetworkManager.LocalClientId != rpcParams.Receive.SenderClientId) return;
	
		WorldManager.Instance.LoadBiome(_startingBiome, Player.LocalClientInstance.transform.position);
	}
	
	#endregion
	
	#region DataBase Functions
	
	public ItemSO GetItemSOFromItemId(int index)
	{
		if(index >= 0 && index < _itemDataBaseSO.ItemSOList.Count)
		{
			return _itemDataBaseSO.ItemSOList[index];
		}
		else
		{
			// Debug.LogWarning($"ItemSO for index: {index} can't be found, returning null");
			return null;
		}
	}
	
	public NpcSO GetNpcSOFromNpcId(int index)
	{
		if(index >= 0 && index < NpcDataBaseSO.NpcDataBase.Count)
		{
			return NpcDataBaseSO.NpcDataBase[index];
		}
		else
		{
			// Debug.LogWarning($"NpcSO for index: {index} can't be found, returning null");
			return null;
		}
	}
	
	public int GetNpcIdFromNpcSO(NpcSO npcSO)
	{
		return NpcDataBaseSO.NpcDataBase.IndexOf(npcSO);
	}
	
	public byte GetTileIdFromTileSO(TileSO tileSO)
	{
		return (byte)_tileDataBaseSO.TileObjectSOList.IndexOf(tileSO);
	}
	
	public int GetTileIdFromTileBase(TileBase tileBase)
	{
		return GetTileIdFromTileSO(GetTileSOFromTileBase(tileBase));
	}
	
	public int GetItemIdFromItemSO(ItemSO item)
	{
		if(item == null)
		{
			return -1;
		}
	
		int index = _itemDataBaseSO.ItemSOList.IndexOf(item);
		if(index > 65535 || index < 0)
		{
			Debug.LogError($"Warning, {item.name} is returning an index value out of bounds of a ushort");
		}
		
		return (ushort)index;
	}
	
	public WorldObject GetWorldObjectFromID(int id)
	{
		return _worldObjectDataBaseSO.WorldObjectList[id];
	}
	
	public int GetIDFromWorldObject(WorldObject worldObject)
	{
		foreach (WorldObject wo in _worldObjectDataBaseSO.WorldObjectList)
		{
			if(wo.WorldObjectName == worldObject.WorldObjectName)
			{
				return _worldObjectDataBaseSO.WorldObjectList.IndexOf(wo);
			}
		}
		
		Debug.LogError($"Cannot find {worldObject} in WorldAssetList. Warning returning 0");
		return 0;
	}
	
	public TileSO GetTileSOFromTileBase(TileBase tileBase)
	{
		foreach (TileSO tileObjectSO in _tileDataBaseSO.TileObjectSOList)
		{
			if(tileObjectSO == tileBase)
			{
				return tileObjectSO;
			}
		}
		
		Debug.LogError($"Cannot find {tileBase} in TileObjectSOList, returning default");
		return default;
	}
	
	public byte GetTileIDFromTilemapTilePosition(Tilemap tilemap, Vector3Int position)
	{
		if(tilemap.HasTile(position))
		{
			return GetIDFromTileObjectSO(tilemap.GetTile(position) as TileSO);
		}
		
		Debug.LogError($"Cannot return tile on tilemap {tilemap.name} on {position} because {tilemap.name} has no tile at that position");
		return default;
	}
	
	public byte GetIDFromTileObjectSO(TileSO tileObjectSO)
	{
		return (byte)_tileDataBaseSO.TileObjectSOList.IndexOf(tileObjectSO);
	}
	
	public TileSO GetTileSOFromID(int id)
	{
		return _tileDataBaseSO.TileObjectSOList.ElementAt(id);
	}
	
	#endregion

	#region Item Functions
	
	public void SpawnItem(InventoryItem inventoryItem, Vector2 spawnPos, BiomeType biome, Vector2 velocity = default)
	{
		if(inventoryItem == null)
		{
			Debug.LogWarning($"Warning, item can't be spawned because it is null");
			return;
		}
	
		SyncItemData syncItemData = new SyncItemData
		{
			ItemId = (ushort)GetItemIdFromItemSO(inventoryItem.Item),
			Quantity = (ushort)inventoryItem.Quantity,
			MagicArray = inventoryItem is SpellbookInventoryItem wandInventoryItem ? wandInventoryItem.MagicArray.Select(x => x != null ? GetItemIdFromItemSO(x) : -1).ToList() : new List<int>()
		};
		
		SpawnItemServerRpc(syncItemData, spawnPos, biome, velocity);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnItemServerRpc(SyncItemData syncItemData, Vector2 spawnPos, BiomeType biome, Vector2 velocity)
	{
		GameObject itemGameObject = Instantiate(_itemBasePrefab, spawnPos, Quaternion.identity);
		
		NetworkObject itemNetworkObject = itemGameObject.GetComponent<NetworkObject>();
		itemNetworkObject.SpawnWithObservers = false;
		itemNetworkObject.Spawn(true);
		
		Item item = itemGameObject.GetComponent<Item>();
		item.Initialize(syncItemData, biome, velocity);
	}
	
	public void DestroyItem(Item itemToDestroy)
	{
		DestroyItemServerRpc(itemToDestroy.NetworkObject);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DestroyItemServerRpc(NetworkObjectReference itemNetworkObjectReference)
	{
		itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject);
		Item item = itemNetworkObject.GetComponent<Item>();
		
		item.DestroySelf();
	}

	#endregion

	#region Damage Number Functions

	private Gradient _gradient;
	private GradientColorKey[] _colorKey;
	private GradientAlphaKey[] _alphaKey;

	[Rpc(SendTo.ClientsAndHost)]
	public void PlayDamageNumbersClientRpc(int amount, Vector2 position, BiomeType biome, Color color)
	{
		if (biome == Player.LocalClientInstance.CurrentPlayerBiome.Value)
		{
			MMF_Player damageNumberFeedbacks = transform.GetChild(0).GetComponent<MMF_Player>();
			MMF_FloatingText floatingText = damageNumberFeedbacks.GetFeedbackOfType<MMF_FloatingText>();

			floatingText.Value = amount.ToString();
			
			// we setup some fancy colors
			_gradient = new Gradient();
			// Populate the color keys at the relative time 0 and 1 (0 and 100%)
			_colorKey = new GradientColorKey[2];
			_colorKey[0].color = color;
			_colorKey[0].time = 0.0f;
			_colorKey[1].color = color;
			_colorKey[1].time = 1.0f;
			// Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
			_alphaKey = new GradientAlphaKey[2];
			_alphaKey[0].alpha = 0.0f;
			_alphaKey[0].time = 0.0f;
			_alphaKey[1].alpha = 1.0f;
			_alphaKey[1].time = 1.0f;
			_gradient.SetKeys(_colorKey, _alphaKey);

			floatingText.ForceColor = true;
			floatingText.AnimateColorGradient = _gradient;
			
			damageNumberFeedbacks.transform.position = position;
			damageNumberFeedbacks.PlayFeedbacks(position);
		}
	}
	
	#endregion
	
	public void InvokeSpawnBreadCrumbEvent(GameObject breadCrumb)
	{
		OnSpawnBreadCrumbPrefab?.Invoke(this, new BreadCrumbEventArgs
		{
		   SpawnedBreadCrumbPrefab = breadCrumb 
		});
	}

	public void TogglePvp(bool pvpEnabled)
	{
		if(Player.LocalClientInstance != null)
		{
			Debug.Log($"Pvp enabled: {pvpEnabled}");
			Player.LocalClientInstance.TogglePvp(pvpEnabled);
		}
	}
	
}
