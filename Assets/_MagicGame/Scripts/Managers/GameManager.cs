using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class GameManager : NetworkBehaviour
{
	public static GameManager Instance { get; private set; }
	
	
	[Title("Item Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private GameObject _itemBasePrefab;
	[SerializeField] private GameObject _playerPrefab;
	[SerializeField] private MiningProjectile _miningProjectilePrefab;
	[SerializeField] private AudioClip _pickupClip;
	
	[Title("Database Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private ItemDataBaseSO _itemDataBaseSO;
	[SerializeField] private WorldObjectDataBaseSO _worldObjectDataBaseSO;
	[SerializeField] private TileDataBaseSO _tileDataBaseSO;
	[SerializeField] private ItemParameterDataBaseSO _itemParameterDataBaseSO;
	[SerializeField] private NpcDataBaseSO _npcDataBaseSO;
	
	private bool _isFirstUpdate = true;
	
	private void Awake()
	{
		Instance = this;
	}
	
	private void OnEnable()
	{
		// Subscribe to the sceneLoaded event
		SceneManager.sceneLoaded += OnSceneLoaded;
		
	}

	private void OnDisable()
	{
		// Unsubscribe from the sceneLoaded event to avoid memory leaks
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	// This function is called whenever a scene has finished loading
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		Debug.Log($"Scene {scene.name} has finished loading.");
		// Debug.Log(HotbarManager.Instance == null);
		// NetworkManager.Singleton.SpawnManager.InstantiateAndSpawn(_playerPrefab.GetComponent<NetworkObject>(), OwnerClientId, isPlayerObject: true, position: new Vector3(128, 128), rotation: Quaternion.identity);
		
		if(NetworkManager.Singleton == null) return;
		NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
		
		if(Loader.IsHost)
		{
			Debug.Log("Host");
			NetworkManager.Singleton.StartHost();
		}
		else
		{
			Debug.Log("Client");
			NetworkManager.Singleton.StartClient();
		}
	}

	private void OnClientConnected(ulong clientId)
	{
		if(clientId == NetworkManager.ServerClientId)
		{
			HandleEnvironment();
		}
	}
	
	private async void HandleEnvironment()
	{
		if(SaveSystem.Instance.EnvironmentDataExists(EnvironmentID.Forest))
		{
			await SaveSystem.Instance.DeserializeAndDispatchData(EnvironmentID.Forest);
		}
		else
		{
			WorldManager.Instance.GenerateEnvironment(EnvironmentID.Forest);
		}
	}

	public NpcSO GetNpcSOFromId(byte id)
	{
		return _npcDataBaseSO.NpcSOList[id];
	}
	
	public byte GetIdAsByteFromNpcSO(NpcSO npcSO)
	{
		int index = _npcDataBaseSO.NpcSOList.IndexOf(npcSO);
		if(index > 255 || index < 0)
		{
			Debug.LogError($"Warning, {npcSO.name} is returning an index value out of bounds of a byte");
		}
		
		return (byte)index;
	}	
	
	public ItemParameterDataBaseSO GetItemParameterDataBaseSO()
	{
		return _itemParameterDataBaseSO;
	}
	
	public byte GetTileIdFromTileSO(TileSO tileSO)
	{
		return (byte)_tileDataBaseSO.TileObjectSOList.IndexOf(tileSO);
	}
	
	public int GetItemIndexFromItemObject(ItemSO item)
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
	
	public byte GetByteIDFromWorldObject(WorldObject worldObject)
	{
		foreach (WorldObject wo in _worldObjectDataBaseSO.WorldObjectList)
		{
			if(wo.GetWorldObjectName() == worldObject.GetWorldObjectName())
			{
				return (byte)_worldObjectDataBaseSO.WorldObjectList.IndexOf(wo);
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
			return GetByteIDFromTileObjectSO(tilemap.GetTile(position) as TileSO);
		}
		
		Debug.LogError($"Cannot return tile on tilemap {tilemap.name} on {position} because {tilemap.name} has no tile at that position");
		return default;
	}
	
	public byte GetByteIDFromTileObjectSO(TileSO tileObjectSO)
	{
		return (byte)_tileDataBaseSO.TileObjectSOList.IndexOf(tileObjectSO);
	}
	
	public TileSO GetTileSOFromID(int id)
	{
		return _tileDataBaseSO.TileObjectSOList.ElementAt(id);
	}
	
	public void SpawnMiningProjectile(Vector2 spawnPoint, Vector2 travelPoint, int miningPower, bool mouseOverFloor, bool mouseOverWall, bool resourceSelected)
	{
		if(!Player.LocalClientInstance.IsHost)
		{
			// If player is not host, spawn the fake projectile and hide the actual server projectile
			MiningProjectile dummyProjectile = Instantiate(_miningProjectilePrefab, spawnPoint, Quaternion.identity);
			dummyProjectile.InitializeMiningSpell(travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected, Player.LocalClientInstance.OwnerClientId);
		}
		
		SpawnMiningProjectileServerRpc(spawnPoint, travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected, Player.LocalClientInstance.OwnerClientId, Player.LocalClientInstance.IsHost);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnMiningProjectileServerRpc(Vector2 spawnPoint, Vector2 travelPoint, int miningPower, bool mouseOverFloor, bool mouseOverWall, bool resourceSelected, ulong clientSenderId, bool isHost)
	{
		MiningProjectile miningProjectile = Instantiate(_miningProjectilePrefab, spawnPoint, Quaternion.identity);
		miningProjectile.GetComponent<NetworkObject>().Spawn(true);
		miningProjectile.InitializeMiningSpell(travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected, clientSenderId);
		
		if(!isHost)
		{
			miningProjectile.GetComponent<NetworkObject>().NetworkHide(clientSenderId);
		}
	}
	
	public void SpawnItem(ItemSO itemToSpawn, int amount, Vector2 spawnPos, bool playAudio = true)
	{
		if(itemToSpawn == null)
		{
			Debug.LogWarning($"Warning, item can't be spawned because it is null");
			return;
		}
	
		int itemId = GetItemIndexFromItemObject(itemToSpawn); 
		ushort itemAmount = (ushort)amount;
	
		SpawnItemServerRpc((ushort)itemId, itemAmount, spawnPos, playAudio);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnItemServerRpc(ushort itemId, ushort itemAmount, Vector2 spawnPos, bool playAudio = true)
	{
		GameObject itemGameObject = Instantiate(_itemBasePrefab, spawnPos, Quaternion.identity);
		
		Item item = itemGameObject.GetComponent<Item>();
		item.SetItemIdAndAmount(itemId, itemAmount);
		
		NetworkObject itemNetworkObject = itemGameObject.GetComponent<NetworkObject>();
		itemNetworkObject.Spawn(true);
		
		if (playAudio)
		{
			MMSoundManagerSoundPlayEvent.Trigger(_pickupClip, MMSoundManager.MMSoundManagerTracks.UI, default);
		}
	}
	
	public void DestroyItem(Item itemToDestroy)
	{
		DestroyItemServerRpc(itemToDestroy.NetworkObject);
	}
	
	public ItemSO GetItemSOFromIndex(int index)
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

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DestroyItemServerRpc(NetworkObjectReference itemNetworkObjectReference)
	{
		itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject);
		Item item = itemNetworkObject.GetComponent<Item>();
		
		item.DestroySelf();
	}
}
