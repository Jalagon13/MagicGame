using System;
using MoreMountains.Tools;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerManager : NetworkBehaviour
{
    public static MultiplayerManager Instance { get; private set; }
	
    [Title("Item Settings", null, TitleAlignments.Centered, HorizontalLine = true, Bold = true)]
    [SerializeField] private ItemDataBaseSO _itemDataBaseSO;
    [SerializeField] private GameObject _itemBasePrefab;
    [SerializeField] private MiningProjectile _miningProjectilePrefab;
    [SerializeField] private AudioClip _pickupClip;
	
    private void Awake()
    {
        Instance = this;
    }
	
    public void SpawnMiningProjectile(Vector2 spawnPoint, Vector2 travelPoint, int miningPower, bool mouseOverFloor, bool mouseOverWall, bool resourceSelected)
    {
        SpawnMiningProjectileServerRpc(spawnPoint, travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected);
    }
	
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void SpawnMiningProjectileServerRpc(Vector2 spawnPoint, Vector2 travelPoint, int miningPower, bool mouseOverFloor, bool mouseOverWall, bool resourceSelected)
    {
        MiningProjectile miningProjectile = Instantiate(_miningProjectilePrefab, spawnPoint, Quaternion.identity);
        miningProjectile.GetComponent<NetworkObject>().Spawn(true);
        miningProjectile.InitializeMiningSpell(travelPoint, miningPower, mouseOverFloor, mouseOverWall, resourceSelected);
    }
	
    public void SpawnItem(ItemSO itemToSpawn, int amount, Vector2 spawnPos, bool playAudio = true)
    {
        if(itemToSpawn == null)
        {
            Debug.LogWarning($"Warning, {itemToSpawn.name} can't be spawned because it is null");
            return;
        }
	
        int itemId = _itemDataBaseSO.GetItemIndexFromItemObject(itemToSpawn); 
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
        return _itemDataBaseSO.ItemSOList[index];
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void DestroyItemServerRpc(NetworkObjectReference itemNetworkObjectReference)
    {
        itemNetworkObjectReference.TryGet(out NetworkObject itemNetworkObject);
        Item item = itemNetworkObject.GetComponent<Item>();
		
        item.DestroySelf();
    }
}
