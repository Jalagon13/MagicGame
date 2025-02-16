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
	
	[Title("Database Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
	[SerializeField] private ItemDataBaseSO _itemDataBaseSO;
	[SerializeField] private WorldObjectDataBaseSO _worldObjectDataBaseSO;
	[SerializeField] private TileDataBaseSO _tileDataBaseSO;
	[SerializeField] private BiomeSpawnParamsSO _biomeSpawnParamsSO;
	
	public Dictionary<ulong, GameObject> FakeProjectilesDict { get; private set; } = new Dictionary<ulong, GameObject>();
	
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

	public void SpawnSpellProjectile(SpellItemSO currentSpellItemSO, BiomeType spawnBiome, Vector2 spawnPoint, Vector2 direction, int speed, int damage, float lifetime, int knockback)
	{
		ulong projectileId = IdGenerator.GenerateRandomId();
		int spellindex = GetItemIdFromItemSO(currentSpellItemSO);
		ulong sourcePlayerId = Player.LocalClientInstance.OwnerClientId;
		
		if(!Player.LocalClientInstance.IsHost)
		{
			// If player is not host, spawn fake projectile, and add it to fake projectiles dictionary
			if(currentSpellItemSO.SpellProjectilePrefab == null)
			{
				Debug.Log($"{currentSpellItemSO} spell projectile is null");
				return;
			}
			
			Spell fakeSpell = Instantiate(currentSpellItemSO.SpellProjectilePrefab, spawnPoint, Quaternion.identity);
			fakeSpell.Initialize(spawnBiome, speed, damage, direction, sourcePlayerId, knockback, lifetime, projectileId);
			fakeSpell.CastSpell();
			RegisterFakeProjectile(projectileId, fakeSpell.gameObject);
		}
		
		
		SpawnSpellProjectileServerRpc(spawnBiome, spellindex, spawnPoint, direction, speed, damage, lifetime, sourcePlayerId, projectileId, knockback);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnSpellProjectileServerRpc(BiomeType spawnBiome, int itemIndex, Vector2 spawnPoint, Vector2 direction, int speed, int damage, float lifetime, ulong sourcePlayerId, ulong projectileId, int knockback)
	{
		var spellPrefab = (GetItemSOFromItemId(itemIndex) as SpellItemSO).SpellProjectilePrefab;
		Spell spell = Instantiate(spellPrefab, spawnPoint, Quaternion.identity);
		
		spell.GetComponent<NetworkObject>().Spawn(true);
		spell.Initialize(spawnBiome, speed, damage, direction, sourcePlayerId, knockback, lifetime, projectileId);
		spell.CastSpell();
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
	
	private void RegisterFakeProjectile(ulong projectileId, GameObject projectileGameObject)
	{
		FakeProjectilesDict.Add(projectileId, projectileGameObject);
	}
	
	private void RemoveFakeProjectile(ulong projectileId)
	{
		if (FakeProjectilesDict.TryGetValue(projectileId, out GameObject fakeProjectile))
		{
			Destroy(fakeProjectile);
			FakeProjectilesDict.Remove(projectileId);
		}
	}
	
	public void SpawnItem(ItemSO itemToSpawn, int amount, Vector2 spawnPos, BiomeType biome)
	{
		if(itemToSpawn == null)
		{
			Debug.LogWarning($"Warning, item can't be spawned because it is null");
			return;
		}
	
		int itemId = GetItemIdFromItemSO(itemToSpawn); 
		ushort itemAmount = (ushort)amount;
		
		SpawnItemServerRpc((ushort)itemId, itemAmount, spawnPos, biome);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	private void SpawnItemServerRpc(ushort itemId, ushort itemAmount, Vector2 spawnPos, BiomeType biome)
	{
		GameObject itemGameObject = Instantiate(_itemBasePrefab, spawnPos, Quaternion.identity);
		
		Item item = itemGameObject.GetComponent<Item>();
		item.Initialize(itemId, itemAmount, biome);
		
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
