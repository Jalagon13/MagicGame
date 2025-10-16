using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace ProjectTinker
{
	public class TerrainTileRenderer : MonoBehaviour
	{
	    [field: SerializeField] public TerrainTilemap TerrainTilemapPrefab { get; private set; }
	    [field: SerializeField] public TerrainTilemap LiquidTerrainTilemapPrefab { get; private set; }
	    [field: Space(10)]
	    [field: SerializeField, Tooltip("Top tiles are highest priority, bottom is lowest")] 
	    public List<TileDataSO> TerrainRenderHierarchy { get; private set; }

	    private readonly Dictionary<TileDataSO, TerrainTilemap> _terrainTilemaps = new();

	    private void Start()
	    {
	        GameWorld.Instance.OnBiomeTransitionEnd += WorldManager_OnBiomeTransitionEnd;
	    }

	    private void WorldManager_OnBiomeTransitionEnd(object sender, EventArgs e)
	    {
	        foreach (TerrainTilemap terrainTilemap in _terrainTilemaps.Values)
	        {
	            terrainTilemap.RefreshTerrainTilemap();
	        }
	    }

	    public void SetTerrainTileData(Vector3Int tilePosition, TileDataSO tileSO)
	    {
	        if (!TerrainRenderHierarchy.Contains(tileSO) && tileSO != null)
	        {
	            Debug.LogError($"TileSO {tileSO.name} not found in TerrainRenderHierarchy.");
	            return;
	        }

	        // Remove the tile if tileSO is null
	        if (tileSO == null)
	        {
	            foreach (var kvp in _terrainTilemaps)
	            {
	                TerrainTilemap terrainTilemap = kvp.Value;

	                if (terrainTilemap.HasTileData(tilePosition))
	                {
	                    terrainTilemap.RemoveTileData(tilePosition);

	                    if (terrainTilemap.IsEmpty())
	                    {
	                        Destroy(terrainTilemap.gameObject);
	                        _terrainTilemaps.Remove(kvp.Key);
	                    }

	                    break;
	                }
	            }

	            return;
	        }

	        // If the Tilemap doesn't exist for this TileSO, create one
	        if (!_terrainTilemaps.TryGetValue(tileSO, out TerrainTilemap map))
	        {
	            map = Instantiate(tileSO.TileType == TileType.Terrain ? TerrainTilemapPrefab : LiquidTerrainTilemapPrefab, transform);
	            map.Initialize(tileSO.DualGridTiles);
	            map.name = tileSO.TileType == TileType.Terrain ? $"TerrainTilemap_{tileSO.name}" : $"LiquidTilemap_{tileSO.name}";
            
	            int order = TerrainRenderHierarchy.IndexOf(tileSO);
	            map.GetComponent<Renderer>().sortingOrder = TerrainRenderHierarchy.Count - order;
	            map.transform.SetSiblingIndex(order);
            
	            if(tileSO.DualGridFillTileMaterial != null)
	                map.GetComponent<Renderer>().material = tileSO.DualGridFillTileMaterial;
                
	            _terrainTilemaps[tileSO] = map;
	        }

	        map.SetTileData(tilePosition);
	    }
    
	    public void ClearAllTerrainTiles()
	    {
	        foreach (var tilemap in _terrainTilemaps.Values.ToList())
	        {
	            Destroy(tilemap.gameObject);
	        }
	        _terrainTilemaps.Clear();
	    }

	    public TileDataSO GetTileData(Vector3Int position)
	    {
	        foreach (var kvp in _terrainTilemaps)
	        {
	            if (kvp.Value.HasTileData(position))
	            {
	                return kvp.Key;
	            }
	        }
	        return null;
	    }

	    public bool HasTile(Vector3Int position)
	    {
	        foreach (TerrainTilemap terrainTilemap in _terrainTilemaps.Values)
	        {
	            if (terrainTilemap.HasTileData(position))
	                return true;
	        }
	        return false;
	    }
    
	    private void OnDestroy()
	    {
	        GameWorld.Instance.OnBiomeTransitionEnd -= WorldManager_OnBiomeTransitionEnd;
	    }
	}
}