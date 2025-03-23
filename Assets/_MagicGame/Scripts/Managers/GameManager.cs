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
	
	private Dictionary<ulong, Spell> _loadedSpells = new Dictionary<ulong, Spell>();
	
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

	#region Spell Projectile Functions
	
	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void LoadSpellServerRpc(SyncSpellData spellData, Vector2 loadPoint)
	{
		Spell spell = Instantiate((GetItemSOFromItemId(spellData.SpellIndex) as SpellItemSO).SpellProjectilePrefab, loadPoint, Quaternion.identity);
		spell.SetSpellData(spellData);
		spell.GetComponent<SpellNetworkComponent>().InitializeSpellNetwork(spellData);
		
		_loadedSpells[spellData.SpellId] = spell;
		
		NetworkObject no = spell.GetComponent<NetworkObject>();
		no.SpawnWithObservers = false;
		no.Spawn(true);
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void ExecuteSpellServerRpc(ulong spellId, Vector2 finalDirection, Vector2 spawnPoint)
	{
	    if(_loadedSpells.ContainsKey(spellId))
	    {
			_loadedSpells[spellId].ExecuteSpellStart(finalDirection, spawnPoint);
		}
		else
		{
		    Debug.LogWarning($"Spell with id {spellId} not found. Can't Execute");
		}
	}

	[Rpc(SendTo.Server, RequireOwnership = false)]
	public void CancelSpellServerRpc(ulong spellId)
	{
		if(_loadedSpells.ContainsKey(spellId))
		{
			_loadedSpells[spellId].CancelSpell();
			Debug.Log($"Spell Canceled");
		}
		else
		{
			Debug.LogWarning($"Spell with id {spellId} not found. Can't Cancel");
		}
	}
	
	#endregion
	
	#region Item Functions
	
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
		itemNetworkObject.SpawnWithObservers = false;
		itemNetworkObject.Spawn(true);
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
	
	public void PlayDamageNumbers(int amount, Vector2 position, BiomeType biome)
	{
		PlayDamageNumbersClibentRpc(amount, position, biome);
	}
	
	[Rpc(SendTo.ClientsAndHost)]
	private void PlayDamageNumbersClibentRpc(int damageAmount, Vector2 position, BiomeType biome)
	{
		if(biome == Player.LocalClientInstance.CurrentPlayerBiome.Value && ChunkManager.Instance.ObjectPositionInLoadedChunks(position))
		{
			MMF_Player damageNumberFeedbacks = transform.GetChild(0).GetComponent<MMF_Player>();
			MMF_FloatingText floatingText = damageNumberFeedbacks.GetFeedbackOfType<MMF_FloatingText>();
	
			floatingText.Value = damageAmount.ToString();
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
