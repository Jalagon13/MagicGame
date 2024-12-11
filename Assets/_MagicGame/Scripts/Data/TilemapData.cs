using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapData : NetworkBehaviour
{
    [SerializeField] private TileDataBaseSO _tileDataBaseSO;
    [SerializeField] private bool _canMine;
    private Tilemap _tilemap;
    private NetworkList<SyncTileHPData> _syncTileHPDataNetworkList;
    public struct SyncTileHPData : IEquatable<SyncTileHPData>, INetworkSerializable
    {
        public byte TileID;
        public ushort CurrentTileHP;
        public Vector2Int Position;

        public bool Equals(SyncTileHPData other)
        {
            return Position.Equals(other.Position) && TileID == other.TileID;
        }
	
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CurrentTileHP);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref TileID);
        }
    }
	
    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        _syncTileHPDataNetworkList = new();
    }

    public void HitTile(Vector2Int position, int amount)
    {
        // Debug.Log("HitTile callback");
        var vector3IntPos = new Vector3Int(position.x, position.y);
		
        if(_tilemap.HasTile(vector3IntPos) && _canMine)
        {
            var tileId = (byte)_tileDataBaseSO.GetTileIDFromTilemapTilePosition(_tilemap, vector3IntPos);
            DamageTileServerRpc(position, tileId, (ushort)amount);
        }
    }
	
    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void DamageTileServerRpc(Vector2Int position, byte tileId, ushort incomingDamage)
    {
        // If it doesn't contain an entry, add it and damage it
        if(!HpDataListContainsPosition(position))
        {
            AddTileToNetworkListDamaged(position, tileId, incomingDamage);
            return;
        }
        // Find the index of the tile in the list
        for (int i = 0; i < _syncTileHPDataNetworkList.Count; i++)
        {
            var syncTileHpData = _syncTileHPDataNetworkList[i];

            if (syncTileHpData.Position == position)
            {
                // If damage is greater than current hp for this incoming attack, destroy the tile
                if (incomingDamage > syncTileHpData.CurrentTileHP)
                {
                    // Remove the tile if destroyed
                    _syncTileHPDataNetworkList.RemoveAt(i);

                    // Make node walkable
                    var centerNodePosition = new Vector2(position.x + 0.5f, position.y + 0.5f);
                    // var node = NodeGraphUtility.GetNodeAtPosition(centerNodePosition);
                    // node.Walkable = true;
					
                    // Trigger tile destruction logic
                    DestroyTile(position, syncTileHpData.TileID);
                }
                else
                {
                    // Update the modified struct in the list
                    syncTileHpData.CurrentTileHP -= incomingDamage;
                    // Debug.Log("Found tile callback, tile hp after damage: " + syncTileHpData.CurrentTileHP);
                    _syncTileHPDataNetworkList[i] = syncTileHpData;
                }

                return; // Exit after finding the tile
            }
        }
    }

    private void AddTileToNetworkListDamaged(Vector2Int position, byte tileId, ushort damageAmount)
    {
        TileSO tileSO = _tileDataBaseSO.GetTileObjectSOFromID(tileId);
		
        // Debug.Log(tileObjectSO.MaxHitPoints);
        // Debug.Log(damageAmount);
        ushort currentTileHpAfterDamage = (ushort)(tileSO.MaxHitPoints - damageAmount);
        if(currentTileHpAfterDamage > 0)
        {
            // If tile hp after damage is above 0, just add as usual
            // Debug.Log($"Added tile for the first time damaged ({currentTileHpAfterDamage})since this is a new entry");
            _syncTileHPDataNetworkList.Add(new SyncTileHPData()
            {
                TileID = (byte)_tileDataBaseSO.GetIDFromTileObjectSO(tileSO),
                CurrentTileHP = currentTileHpAfterDamage,
                Position = position
            });
        }
        else
        {
            // If tile hp is destroyed, destroy tile
            DestroyTile(position, tileId);
        }
    }
	
    private void DestroyTile(Vector2Int position, byte tileId)
    {
        ItemSO dropItem = _tileDataBaseSO.TileObjectSOList[tileId].DropItem;
        MultiplayerManager.Instance.SpawnItem(dropItem, 3, position);
			
        DestroyTileClientRpc(position);
    }

    [Rpc(SendTo.Everyone)]
    private void DestroyTileClientRpc(Vector2Int position)
    {
        // Debug.Log("Destroying tile instead of adding entry because it was destroyed in one hit");
        var pos = new Vector3Int(position.x, position.y, 0);
		
        // If tile has tile, then the chunk is loaded, if not, no need to destroy anything since there is no tile to destroy
        if(_tilemap.HasTile(pos))
        {
            _tilemap.SetTile(pos, null);
        }
	
        ChunkManager.Instance.RemoveWallTileDataFromChunk(position);
    }
	
    public WandAttribute GetHarvestType(Vector2Int position)
    {
        return _tileDataBaseSO.GetTileObjectSOFromID(GetSyncTileHpDataFromPosition(position).TileID).HarvestType;
    }
	
    // NTFS: This could be bugged
    public void DeleteTile(Vector2Int position)
    {
        var vector3IntPos = new Vector3Int(position.x, position.y);
	
        if(_tilemap.HasTile(vector3IntPos))
        {
            if(HpDataListContainsPosition(position))
            {
                _tilemap.SetTile(vector3IntPos, null);
				
                _syncTileHPDataNetworkList.Remove(GetSyncTileHpDataFromPosition(position));
				
                ChunkManager.Instance.RemoveWallTileDataFromChunk(position);
            }
        }
    }
	
    private bool HpDataListContainsPosition(Vector2Int position)
    {
        foreach (SyncTileHPData hpData in _syncTileHPDataNetworkList)
        {
            if(hpData.Position == position)
            {
                return true;
            }
        }
		
        return false;
    }
	
    private SyncTileHPData GetSyncTileHpDataFromPosition(Vector2Int position)
    {
        foreach (SyncTileHPData hpData in _syncTileHPDataNetworkList)
        {
            if(hpData.Position == position)
            {
                return hpData;
            }
        }
		
        return default;
    }
	
    public Tilemap GetTilemap()
    {
        return _tilemap;
    }
}
