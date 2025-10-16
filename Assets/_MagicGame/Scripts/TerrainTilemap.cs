using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace ProjectTinker
{
	public class TerrainTilemap : MonoBehaviour
	{
	    private static readonly Vector3Int[] NEIGHBORS = new Vector3Int[] {
	        new Vector3Int(0, 0, 0),
	        new Vector3Int(1, 0, 0),
	        new Vector3Int(0, 1, 0),
	        new Vector3Int(1, 1, 0)
	    };

	    private readonly HashSet<Vector2Int> _dataTilePositions = new();
	    private Tilemap _displayTilemap;
	    private Dictionary<Tuple<int, int, int, int>, TileBase> _neighborTupleToTile;

	    private void Awake()
	    {
	        _displayTilemap = GetComponent<Tilemap>();
	    }
    
	    public void Initialize(TileBase[] tiles)
	    {
	        if(tiles.Length != 16)
	        {
	            Debug.LogError($"Expected 16 tiles, received {tiles.Length}");
	        }
    
	        _neighborTupleToTile = new()
	        {
	            {new (1, 1, 1, 1), tiles[6]},
	            {new (0, 0, 0, 1), tiles[13]}, // OUTER_BOTTOM_RIGHT
	            {new (0, 0, 1, 0), tiles[0]}, // OUTER_BOTTOM_LEFT
	            {new (0, 1, 0, 0), tiles[8]}, // OUTER_TOP_RIGHT
	            {new (1, 0, 0, 0), tiles[15]}, // OUTER_TOP_LEFT
	            {new (0, 1, 0, 1), tiles[1]}, // EDGE_RIGHT
	            {new (1, 0, 1, 0), tiles[11]}, // EDGE_LEFT
	            {new (0, 0, 1, 1), tiles[3]}, // EDGE_BOTTOM
	            {new (1, 1, 0, 0), tiles[9]}, // EDGE_TOP
	            {new (0, 1, 1, 1), tiles[5]}, // INNER_BOTTOM_RIGHT
	            {new (1, 0, 1, 1), tiles[2]}, // INNER_BOTTOM_LEFT
	            {new (1, 1, 0, 1), tiles[10]}, // INNER_TOP_RIGHT
	            {new (1, 1, 1, 0), tiles[7]}, // INNER_TOP_LEFT
	            {new (0, 1, 1, 0), tiles[14]}, // DUAL_UP_RIGHT
	            {new (1, 0, 0, 1), tiles[4]}, // DUAL_DOWN_RIGHT
	            {new (0, 0, 0, 0), tiles[12]},
	        };
	    }
    
	    public void SetTileData(Vector3Int tilePosition)
	    {
	        _dataTilePositions.Add(new Vector2Int(tilePosition.x, tilePosition.y));
	    }
    
	    public void RemoveTileData(Vector3Int tilePosition)
	    {
	        _dataTilePositions.Remove(new Vector2Int(tilePosition.x, tilePosition.y));
	    }
    
	    public bool HasTileData(Vector3Int tilePosition)
	    {
	        return _dataTilePositions.Contains(new Vector2Int(tilePosition.x, tilePosition.y));
	    }
    
	    public bool IsEmpty()
	    {
	        return _dataTilePositions.Count <= 0;
	    }
    
	    private int GetTileTypeAt(Vector3Int pos)
	    {
	        return HasTileData(pos) ? 1 : 0; // 1 for grass, 0 for dirt
	    }

	    private TileBase CalculateDisplayTile(Vector3Int pos)
	    {
	        int topRight = GetTileTypeAt(pos - NEIGHBORS[0]);
	        int topLeft = GetTileTypeAt(pos - NEIGHBORS[1]);
	        int botRight = GetTileTypeAt(pos - NEIGHBORS[2]);
	        int botLeft = GetTileTypeAt(pos - NEIGHBORS[3]);

	        var tuple = new Tuple<int, int, int, int>(topLeft, topRight, botLeft, botRight);

	        if (_neighborTupleToTile.TryGetValue(tuple, out var tile))
	            return tile;

	        return null;
	    }

	    public void RefreshTerrainTilemap()
	    {
	        foreach (var pos in _dataTilePositions)
	        {
	            Vector3Int tilePos = new(pos.x, pos.y, 0);

	            foreach (var offset in NEIGHBORS)
	            {
	                Vector3Int displayPos = tilePos + offset;
	                _displayTilemap.SetTile(displayPos, CalculateDisplayTile(displayPos));
	            }
	        }
	    }

	    internal void Initialize(object dualGridTiles)
	    {
	        throw new NotImplementedException();
	    }
	}

}