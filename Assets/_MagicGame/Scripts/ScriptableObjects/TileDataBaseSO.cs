using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// [CreateAssetMenu()]
public class TileDataBaseSO : ScriptableObject
{
    public List<TileSO> TileObjectSOList;
	
    public TileSO GetTileObjectFromTileBase(TileBase tileBase)
    {
        foreach (TileSO tileObjectSO in TileObjectSOList)
        {
            if(tileObjectSO == tileBase)
            {
                return tileObjectSO;
            }
        }
		
        Debug.LogError($"Cannot find {tileBase} in TileObjectSOList, returning default");
        return default;
    }
	
    public int GetTileIDFromTilemapTilePosition(Tilemap tilemap, Vector3Int position)
    {
        if(tilemap.HasTile(position))
        {
            return GetIDFromTileObjectSO(tilemap.GetTile(position) as TileSO);
        }
		
        Debug.LogError($"Cannot return tile on tilemap {tilemap.name} on {position} because {tilemap.name} has no tile at that position");
        return -1;
    }
	
    public int GetIDFromTileObjectSO(TileSO tileObjectSO)
    {
        return TileObjectSOList.IndexOf(tileObjectSO);
    }
	
    public TileSO GetTileObjectSOFromID(int id)
    {
        return TileObjectSOList.ElementAt(id);
    }
}
