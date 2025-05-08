using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainTileRenderer : MonoBehaviour
{
    [field: SerializeField] public TerrainTilemap TerrainTilemapPrefab { get; private set; }
    [field: Space(10)]
    [field: SerializeField, Tooltip("Top tiles are highest priority, bottom is lowest")] 
    public List<TileSO> TerrainRenderHierarchy { get; private set; }

    private readonly Dictionary<TileSO, TerrainTilemap> _tilemaps = new();

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
            foreach (var kvp in _tilemaps)
            {
                TerrainTilemap terrainTilemap = kvp.Value;

                if (terrainTilemap.HasTileData(tilePosition))
                {
                    terrainTilemap.RemoveTileData(tilePosition);

                    if (terrainTilemap.IsEmpty())
                    {
                        Destroy(terrainTilemap.gameObject);
                        _tilemaps.Remove(kvp.Key);
                    }

                    break;
                }
            }

            return;
        }

        // If the Tilemap doesn't exist for this TileSO, create one
        if (!_tilemaps.TryGetValue(tileSO, out TerrainTilemap map))
        {
            map = Instantiate(TerrainTilemapPrefab, transform);
            map.Initialize(tileSO.DualGridTiles);
            map.name = $"Tilemap_{tileSO.name}";
            int order = TerrainRenderHierarchy.IndexOf(tileSO);
            map.GetComponent<Renderer>().sortingOrder = TerrainRenderHierarchy.Count - order;
            map.transform.SetSiblingIndex(order);
            _tilemaps[tileSO] = map;
        }

        map.SetTileData(tilePosition);
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
        foreach (TerrainTilemap terrainTilemap in _tilemaps.Values)
        {
            if (terrainTilemap.HasTileData(position))
                return true;
        }
        return false;
    }
}
