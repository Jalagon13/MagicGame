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
	
	[SerializeField] private BiomeType _startingBiome;
	
	[Title("Item Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private GameObject _itemBasePrefab;
	[SerializeField] private GameObject _playerPrefab;
	[SerializeField] private MiningProjectile _miningProjectilePrefab;
	
	[Title("Database Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private ItemDataBaseSO _itemDataBaseSO;
	[SerializeField] private WorldObjectDataBaseSO _worldObjectDataBaseSO;
	[SerializeField] private TileDataBaseSO _tileDataBaseSO;
	[SerializeField] private ItemParameterDataBaseSO _itemParameterDataBaseSO;
	[SerializeField] private BiomeSpawnParamsSO _biomeSpawnParamsSO;
	
	private Dictionary<ulong, GameObject> _fakeProjectiles = new Dictionary<ulong, GameObject>();
	
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
		if(NetworkManager.Singleton == null) return;
		
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
		if(clientId == NetworkManager.ServerClientId)
		{
			WorldManager.Instance.LoadBiome(_startingBiome, Player.LocalClientInstance.transform.position, false);
		}
	}
	
	
	#region DataBase Functions
	
	public NpcSpawnData GetNpcSpawnData(BiomeType biome, int id)
	{
		return _biomeSpawnParamsSO.GetBiomeSpawnRule(biome).NpcSpawnTable[id];
	}
	
	public int GetNpcIdFromNpcSpawnData(BiomeType biome, NpcSpawnData npcSpawnData)
	{
		return _biomeSpawnParamsSO.GetBiomeSpawnRule(biome).NpcSpawnTable.IndexOf(npcSpawnData);
	}	
	
	public ItemParameterDataBaseSO GetItemParameterDataBaseSO()
	{
		return _itemParameterDataBaseSO;
	}
	
	public byte GetTileIdFromTileSO(TileSO tileSO)
	{
		return (byte)_tileDataBaseSO.TileObjectSOList.IndexOf(tileSO);
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
	
	#endregion

	public void SpawnSpellProjectile(SpellProjectileItemSO currentSpellItemSO, Vector2 spawnPoint, Vector2 direction, int speed, int damage, float lifetime)
	{
		ulong projectileId = IdGenerator.GenerateRandomId();
		
		if(!Player.LocalClientInstance.IsHost)
		{
			// If player is not host, spawn fake projectile, and add it to fake projectiles dictionary
			BouncySpellProjectile fakeSpell = Instantiate(currentSpellItemSO.SpellProjectilePrefab, spawnPoint, Quaternion.identity);
			fakeSpell.Initialize(speed, damage, lifetime,direction, Player.LocalClientInstance.OwnerClientId, projectileId);
			
			AddFakeProjectile(projectileId, fakeSpell.gameObject);
		}
		
		SpawnSpellProjectileServerRpc(GetItemIdFromItemSO(currentSpellItemSO), spawnPoint, direction, speed, damage, lifetime, Player.LocalClientInstance.IsHost, Player.LocalClientInstance.OwnerClientId, projectileId);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnSpellProjectileServerRpc(int itemIndex, Vector2 spawnPoint, Vector2 direction, int speed, int damage, float lifetime, bool isHost, ulong sourcePlayerId, ulong projectileId)
	{
		var spellPrefab = (GetItemSOFromItemId(itemIndex) as SpellProjectileItemSO).SpellProjectilePrefab;
		
		BouncySpellProjectile spell = Instantiate(spellPrefab, spawnPoint, Quaternion.identity);
		
		spell.GetComponent<NetworkObject>().Spawn(true);
		
		spell.Initialize(speed, damage, lifetime,direction, sourcePlayerId, projectileId);
		
		if(!isHost)
		{
			spell.GetComponent<NetworkObject>().NetworkHide(sourcePlayerId);
		}
	}
	
	public void DestroyFakeProjectile(ulong sourcePlayerId, ulong projectileId)
	{
		DestroyFakeProjectileClientRpc(projectileId, RpcTarget.Single(sourcePlayerId, RpcTargetUse.Persistent));
	}
	
	[Rpc(SendTo.SpecifiedInParams)]
	private void DestroyFakeProjectileClientRpc(ulong projectileId, RpcParams rpcParams = default)
	{
		RemoveFakeProjectile(projectileId);
	}
	
	private void AddFakeProjectile(ulong projectileId, GameObject projectileGameObject)
	{
		_fakeProjectiles.Add(projectileId, projectileGameObject);
	}
	
	private void RemoveFakeProjectile(ulong projectileId)
	{
		if (_fakeProjectiles.TryGetValue(projectileId, out GameObject fakeProjectile))
		{
			Debug.Log($"Fake projectile found and destroyed for proj id {projectileId}");
			Destroy(fakeProjectile);
			_fakeProjectiles.Remove(projectileId);
		}
	}

	public void SpawnMiningProjectile(Vector2 spawnPoint, Vector2 travelPoint, int miningPower, bool mouseOverFloor, bool mouseOverWall, bool resourceSelected)
	{
		if(!Player.LocalClientInstance.IsHost)
		{
			// If player is not host, spawn the fake projectile and hide the actual server projectile
			MiningProjectile dummyProjectile = Instantiate(_miningProjectilePrefab, spawnPoint, Quaternion.identity);
			dummyProjectile.InitializeMiningSpell(travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected);
		}
		
		SpawnMiningProjectileServerRpc(spawnPoint, travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected, Player.LocalClientInstance.OwnerClientId, Player.LocalClientInstance.IsHost);
	}
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnMiningProjectileServerRpc(Vector2 spawnPoint, Vector2 travelPoint, int miningPower, bool mouseOverFloor, bool mouseOverWall, bool resourceSelected, ulong clientSenderId, bool isHost)
	{
		MiningProjectile miningProjectile = Instantiate(_miningProjectilePrefab, spawnPoint, Quaternion.identity);
		
		miningProjectile.GetComponent<NetworkObject>().Spawn(true);
		
		miningProjectile.InitializeMiningSpell(travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected);
		
		if(!isHost)
		{
			miningProjectile.GetComponent<NetworkObject>().NetworkHide(clientSenderId);
		}
	}
	
	public void SpawnItem(ItemSO itemToSpawn, int amount, Vector2 spawnPos)
	{
		if(itemToSpawn == null)
		{
			Debug.LogWarning($"Warning, item can't be spawned because it is null");
			return;
		}
	
		int itemId = GetItemIdFromItemSO(itemToSpawn); 
		ushort itemAmount = (ushort)amount;
		
		SpawnItemServerRpc((ushort)itemId, itemAmount, spawnPos);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnItemServerRpc(ushort itemId, ushort itemAmount, Vector2 spawnPos)
	{
		GameObject itemGameObject = Instantiate(_itemBasePrefab, spawnPos, Quaternion.identity);
		
		Item item = itemGameObject.GetComponent<Item>();
		item.SetItemIdAndAmount(itemId, itemAmount);
		
		NetworkObject itemNetworkObject = itemGameObject.GetComponent<NetworkObject>();
		itemNetworkObject.Spawn(true);
	}
	
	public void DestroyItem(Item itemToDestroy)
	{
		DestroyItemServerRpc(itemToDestroy.NetworkObject);
	}
	
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

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void DestroyItemServerRpc(NetworkObjectReference itemNetworkObjectReference)
	{
		itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject);
		Item item = itemNetworkObject.GetComponent<Item>();
		
		item.DestroySelf();
	}
}
