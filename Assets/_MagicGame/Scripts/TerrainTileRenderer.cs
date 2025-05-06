using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainTileRenderer : MonoBehaviour
{
    [field: SerializeField] public Tilemap TerrainTilemapPrefab { get; private set; }
    [field: Space(10)]
    [field: SerializeField, Tooltip("Top tiles are highest priority, bottom is lowest")] 
    public List<TileSO> TerrainRenderHierarchy { get; private set; }

    private readonly Dictionary<TileSO, Tilemap> _tilemaps = new();

    public void RenderTerrainTile(Vector3Int tilePosition, TileSO tileSO)
    {
        if (!TerrainRenderHierarchy.Contains(tileSO) && tileSO != null)
        {
            Debug.LogError($"TileSO {tileSO.name} not found in TerrainRenderHierarchy.");
            return;
        }

        // Remove the tile if tileSO is null
        if (tileSO == null)
        {
            TileSO keyToRemove = null;

            foreach (var kvp in _tilemaps)
            {
                Tilemap tilemap = kvp.Value;
                tilemap.SetTile(tilePosition, null);

                if (IsTilemapEmpty(tilemap))
                {
                    Destroy(tilemap.gameObject);
                    keyToRemove = kvp.Key;
                    break;
                }
            }

            if (keyToRemove != null)
            {
                _tilemaps.Remove(keyToRemove);
            }

            return;
        }

        // If the Tilemap doesn't exist for this TileSO, create one
        if (!_tilemaps.TryGetValue(tileSO, out Tilemap map))
        {
            map = Instantiate(TerrainTilemapPrefab, transform);
            map.name = $"Tilemap_{tileSO.name}";
            int order = TerrainRenderHierarchy.IndexOf(tileSO);
            map.GetComponent<Renderer>().sortingOrder = TerrainRenderHierarchy.Count - order;
            map.transform.SetSiblingIndex(order);
            _tilemaps[tileSO] = map;
        }

        map.SetTile(tilePosition, tileSO);
    }
    
    public void ClearAllTerrainTiles()
    {
        foreach (var tilemap in _tilemaps.Values.ToList())
        {
            Destroy(tilemap.gameObject);
        }
        _tilemaps.Clear();
    }
    
    public bool HasTile(Vector3Int position)
    {
        foreach (var tilemap in _tilemaps.Values)
        {
            if (tilemap.HasTile(position))
                return true;
        }
        return false;
    }

    private bool IsTilemapEmpty(Tilemap tilemap)
    {
        BoundsInt bounds = tilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
                return false;
        }
        return true;
    }
}
