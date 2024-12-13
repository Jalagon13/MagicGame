using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Environment : NetworkBehaviour
{
    public static Environment Instance;

    [SerializeField] private TilemapData _groundTilemapData;
    [SerializeField] private TilemapData _floorTilemapData;
    [SerializeField] private TilemapData _wallTilemapData;

    private void Awake()
    {
        Instance = this;
    }
	
    private void Start()
    {
        ChunkManager.Instance.OnLoadChunk += ChunkManager_OnLoadChunk;
        ChunkManager.Instance.OnUnloadChunk += ChunkManager_OnUnloadChunk;
    }

    private void ChunkManager_OnLoadChunk(object sender, ChunkManager.ChunkEventArgs e)
    {
        // Loop through all ground tiles and set them on tilemap
        foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
        {
            var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
            _groundTilemapData.GetTilemap().SetTile(tilePosV3Int, tile.TileSO);
        }
			
        // loop through all wall tiles and set them on tilemap
        foreach(TileGameData tile in e.Chunk.WallTileGameDataList)
        {
            var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
            _wallTilemapData.GetTilemap().SetTile(tilePosV3Int, tile.TileSO);
        }
    }

    private void ChunkManager_OnUnloadChunk(object sender, ChunkManager.ChunkEventArgs e)
    {
        // Loop through all ground tiles and set null on tilemap
        foreach(TileGameData tile in e.Chunk.GroundTileGameDataList)
        {
            var tilePosV3Int = new Vector3Int(tile.TilePosition.x, tile.TilePosition.y);
		
            _groundTilemapData.GetTilemap().SetTile(tilePosV3Int, null);
			
            if(_wallTilemapData.GetTilemap().HasTile(tilePosV3Int))
            {
                _wallTilemapData.GetTilemap().SetTile(tilePosV3Int, null);
            }
			
            // NTFS: Do the same for floor tilemaps
        }
    }
	
    // Handles placing the visual of the tile, NOT the tile data that is being synced
    public void PlaceTile(Vector3Int pos, TileSO wallTile, TileType syncTileType)
    {
        // Debug.Log("Some Client is placing a tile");
        Vector2Int syncPos = new(pos.x, pos.y);
        byte syncTileId = GameManager.Instance.GetByteIDFromTileObjectSO(wallTile);
		
        PlaceTileVisualServerRpc(syncPos, syncTileId, syncTileType);
    }

    [Rpc(SendTo.Server, RequireOwnership = false)]
    private void PlaceTileVisualServerRpc(Vector2Int syncPos, byte syncTileId, TileType syncTileType)
    {
        // Debug.Log("Server is adding tile data to official world data");
        ChunkManager.Instance.AddWallTileDataToChunk(new(syncPos.x, syncPos.y), syncTileId);
		
        // Update pathfinding
        var centerNodePosition = new Vector2(syncPos.x + 0.5f, syncPos.y + 0.5f);
        var node = NodeGraphUtility.GetNodeAtPosition(centerNodePosition);
        node.Walkable = false;
		
        PlaceTileVisualClientRpc(syncPos, syncTileId, syncTileType);
    }
	
    [Rpc(SendTo.Everyone)]
    private void PlaceTileVisualClientRpc(Vector2Int syncPos, byte syncTileId, TileType syncTileType)
    {
        // Debug.Log("Distributing visual placement information for each client to decide if it is worth placing based on chunks being loaded");
        Vector3Int position = new(syncPos.x, syncPos.y);
        TileSO tileToPlace = GameManager.Instance.GetTileSOFromID(syncTileId);

        // If ground tilemap has a tile at this location, that means the chunk is loaded and is able to accept visual changes
        if(_groundTilemapData.GetTilemap().HasTile(position))
        {
            // Chunk is loaded visually, therefore visually update whatever tile wants to be updated
            switch(syncTileType)
            {
                case TileType.Ground:
                    break;
                case TileType.Floor:
                    _floorTilemapData.GetTilemap().SetTile(position, tileToPlace);
                    break;
                case TileType.Wall:
                    _wallTilemapData.GetTilemap().SetTile(position, tileToPlace);
                    break;
            }
        }
    }

    private void ClearTilemaps()
    {
        _groundTilemapData.GetTilemap().ClearAllTiles();
        _floorTilemapData.GetTilemap().ClearAllTiles();
        _wallTilemapData.GetTilemap().ClearAllTiles();
    }
	
    public TilemapData GetGroundTilemapData()
    {
        return _groundTilemapData;
    }
	
    public TilemapData GetFloorTilemapData()
    {
        return _floorTilemapData;
    }
	
    public TilemapData GetWallTilemapData()
    {
        return _wallTilemapData;
    }
	
    public override void OnDestroy()
    {
        base.OnDestroy();
        ChunkManager.Instance.OnLoadChunk -= ChunkManager_OnLoadChunk;
        ChunkManager.Instance.OnUnloadChunk -= ChunkManager_OnUnloadChunk;
    }
}
