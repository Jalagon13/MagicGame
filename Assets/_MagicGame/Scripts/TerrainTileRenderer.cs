using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainTileRenderer : MonoBehaviour
{
    [field: SerializeField] public TerrainTilemap TerrainTilemapPrefab { get; private set; }
    [field: SerializeField] public TerrainTilemap LiquidTerrainTilemapPrefab { get; private set; }
    [field: Space(10)]
    [field: SerializeField, Tooltip("Top tiles are highest priority, bottom is lowest")] 
    public List<TileSO> TerrainRenderHierarchy { get; private set; }

    private readonly Dictionary<TileSO, TerrainTilemap> _tilemaps = new();

    private void Start()
    {
        WorldManager.Instance.OnBiomeTransitionEnd += WorldManager_OnBiomeTransitionEnd;
    }

    private void WorldManager_OnBiomeTransitionEnd(object sender, EventArgs e)
    {
        foreach (TerrainTilemap terrainTilemap in _tilemaps.Values)
        {
            terrainTilemap.RefreshTerrainTilemap();
        }
        Debug.Log($"Refreshed terrain tilemaps");
    }

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
            map = Instantiate(tileSO.TileType == TileType.Terrain ? TerrainTilemapPrefab : LiquidTerrainTilemapPrefab, transform);
            map.Initialize(tileSO.DualGridTiles);
            map.name = tileSO.TileType == TileType.Terrain ? $"TerrainTilemap_{tileSO.name}" : $"LiquidTilemap_{tileSO.name}";
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
    
    private void OnDestroy()
    {
        WorldManager.Instance.OnBiomeTransitionEnd -= WorldManager_OnBiomeTransitionEnd;
    }
}
