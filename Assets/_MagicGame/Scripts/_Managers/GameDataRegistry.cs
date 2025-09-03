using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameDataRegistry : MonoBehaviour
{
    public static GameDataRegistry Instance { get; private set; }
    
    [SerializeField] 
    private List<CharacterDataSO> _characterData;
    [Space(15)]
    [SerializeField] 
    private List<ResourceDataSO> _resourceData;

    [Space(15)]
    [SerializeField]
    private List<TileDataSO> _tileData;

    [Space(15)]
    [SerializeField]
    private List<ItemDataSO> _itemData;

    private void Awake()
    {
        Instance = this;
    }
    
    #region Item Data Functions
    
    public ushort GetUShortIdFromItemData(ItemDataSO itemData)
    {
        if (itemData == null)
        {
            Debug.LogError($"ItemDataSO is null. Use this log to deduce where this came from");
        }

        for (int i = 0; i < _itemData.Count; i++)
        {
            if (_itemData[i].StringID == itemData.StringID)
            {
                return (ushort)i;
            }
        }

        Debug.LogError($"ItemDataSO '{itemData}' not found!");
        return ushort.MaxValue;
    }
    
    public ItemDataSO GetItemDataFromUShortId(ushort itemId)
    {
        if (itemId >= _itemData.Count || itemId < 0)
        {
            Debug.LogError($"Invalid Item ID: {itemId}");
            return null;
        }

        return _itemData[itemId];
    }
    
    #endregion
    
    #region Tile Data Functions
    
    public ushort GetUShortIdFromTileData(TileDataSO tileData)
    {
        if(tileData == null)
        {
            Debug.LogError($"TileDataSO is null. Use this log to deduce where this came from");
        }

        for (int i = 0; i < _tileData.Count; i++)
        {
            if (_tileData[i].StringID == tileData.StringID)
            {
                return (ushort)i;
            }
        }

        Debug.LogError($"TileDataSO '{tileData}' not found!");
        return ushort.MaxValue;
    }
    
    public ushort GetUShortIdFromTileBase(TileBase tileBase)
    {
        return GetUShortIdFromTileData(GetTileDataFromTileBase(tileBase));
    }
    
    public TileDataSO GetTileDataFromTileBase(TileBase tileBase)
    {
        foreach (TileDataSO tileObjectSO in _tileData)
        {
        	if(tileObjectSO == tileBase)
        	{
        		return tileObjectSO;
        	}
        }

        Debug.LogError($"Cannot find {tileBase} in TileObjectSOList, returning default");
        return default;
    }

    public ushort GetUShortIdFromTilemapTilePosition(Tilemap tilemap, Vector3Int position)
    {
        if (tilemap.HasTile(position))
        {
            return GetUShortIdFromTileBase(tilemap.GetTile(position));
        }

        Debug.LogError($"Cannot return tile on tilemap {tilemap.name} on {position} because {tilemap.name} has no tile at that position");
        return default;
    }

    public TileDataSO GetTileDataFromUShortId(ushort tileId)
    {
        if (tileId >= _tileData.Count || tileId < 0)
        {
            Debug.LogError($"Invalid Tile ID: {tileId}");
            return null;
        }

        return _tileData[tileId];
    }

    #endregion
    
    #region Resource Data Functions

    public ushort GetUShortIdFromResourceData(ResourceDataSO resourceData)
    {
        if(resourceData == null)
        {
            Debug.LogError($"ResourceDataSO is null. Use this log to deduce where this came from");
        }
    
        for (int i = 0; i < _resourceData.Count; i++)
        {
            if (_resourceData[i].StringID == resourceData.StringID)
            {
                return (ushort)i;
            }
        }

        Debug.LogError($"ResourceDataSO '{resourceData}' not found!");
        return ushort.MaxValue;
    }

    public ResourceDataSO GetResourceDataFromUShortId(ushort resourceId)
    {
        if (resourceId >= _resourceData.Count)
        {
            Debug.LogError($"Invalid Resource ID: {resourceId}");
            return null;
        }

        return _resourceData[resourceId];
    }

    #endregion

    #region Character Data Functions

    public ushort GetUShortIdFromCharacterData(CharacterDataSO characterData)
    {
        if (characterData == null)
        {
            Debug.LogError($"CharacterDataSO is null. Use this log to deduce where this came from");
        }

        for (int i = 0; i < _characterData.Count; i++)
        {
            if (_characterData[i].StringID == characterData.StringID)
            {
                return (ushort)i;
            }
        }
        
        Debug.LogError($"CharacterDataSO '{characterData}' not found!");
        return ushort.MaxValue;
    }

    public CharacterDataSO GetCharacterDataFromUShortId(ushort npcId)
    {
        if (npcId >= _characterData.Count)
        {
            Debug.LogError($"Invalid NPC ID: {npcId}");
            return null;
        }

        return _characterData[npcId];
    }
    
    #endregion
}
