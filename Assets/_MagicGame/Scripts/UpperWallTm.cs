using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class UpperWallTm : MonoBehaviour
{
    private enum TopTileType { Single, Right, Left, Center }
    private Tilemap _upperWallTm;
    private Tilemap _upperOreWallTm;

    private void Awake()
    {
        _upperWallTm = GetComponent<Tilemap>();
        _upperOreWallTm = transform.parent.transform.GetChild(1).GetComponent<Tilemap>();
    }

    private void Start()
    {
        GameWorld.Instance.OnBiomeTransitionEnd += RefreshUpperWallTiles;
    }


    public bool IsOverTopTile(Vector2 mousePosition)
    {
        Vector3Int mouseTilePosition = _upperWallTm.WorldToCell(mousePosition);
        if (!_upperWallTm.HasTile(mouseTilePosition))
            return false;

        Vector3 tileWorldPos = _upperWallTm.CellToWorld(mouseTilePosition);
        float tileHeight = _upperWallTm.cellSize.y;

        if (mousePosition.y < tileWorldPos.y + tileHeight / 2f)
            return true;

        return false;
    }

    public void EnableTilemapCollider(bool v)
    {
        _upperWallTm.GetComponent<TilemapCollider2D>().enabled = v;
        _upperOreWallTm.GetComponent<TilemapRenderer>().enabled = v;
    }
    
    public void DeleteUpperWallTile(Vector3Int tilePos)
    {
        Vector3Int upperPosition = new Vector3Int(tilePos.x, tilePos.y + 1, 0);
        _upperWallTm.SetTile(upperPosition, null);
        _upperOreWallTm.SetTile(upperPosition, null);

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                Vector3Int neighborPos = new Vector3Int(tilePos.x + dx, tilePos.y + dy, 0);
                if (TileManager.Instance.WallTm.HasTile(neighborPos))
                {
                    TryToRenderUpperWallTile(neighborPos.x, neighborPos.y);
                }
            }
        }
    }
    
    public void TryToRenderSurroundingUpperWallTiles(Vector3Int tilePos)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                Vector3Int neighborPos = new Vector3Int(tilePos.x + dx, tilePos.y + dy, 0);
                if (TileManager.Instance.WallTm.HasTile(neighborPos))
                {
                    TryToRenderUpperWallTile(neighborPos.x, neighborPos.y);
                }
            }
        }
    }

    public void ClearAllTopTiles()
    {
        _upperWallTm.ClearAllTiles();
        _upperOreWallTm.ClearAllTiles();
    }

    private void RefreshUpperWallTiles(object sender, EventArgs e)
    {
        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                if(TileManager.Instance.WallTm.HasTile(new Vector3Int(x, y, 0)))
                {
                    TryToRenderUpperWallTile(x, y);
                }
            }
        }
        Debug.Log($"Refreshed upper wall tiles");
    }

    private void TryToRenderUpperWallTile(int x, int y)
    {
        TileDataSO tileAtPosition = GameDataRegistry.Instance.GetTileDataFromTileBase(TileManager.Instance.WallTm.GetTile(new Vector3Int(x, y, 0)));
        bool tileExistsAbove = TileManager.Instance.WallTm.HasTile(new Vector3Int(x, y + 1, 0));
        
        if(tileExistsAbove)
        {
            TileDataSO aboveTileSO = GameDataRegistry.Instance.GetTileDataFromTileBase(TileManager.Instance.WallTm.GetTile(new Vector3Int(x, y + 1, 0)));

            if(GameDataRegistry.Instance.GetTileIdFromTileData(tileAtPosition) != GameDataRegistry.Instance.GetTileIdFromTileData(aboveTileSO))
            {
                RenderUpperWallTile(tileAtPosition, x, y);
            }
        }
        else
        {
            RenderUpperWallTile(tileAtPosition, x, y);
        }
    }

    private void RenderUpperWallTile(TileDataSO tileAtPosition, int x, int y)
    {
        Vector3Int tilePosition = new Vector3Int(x, y, 0);
        Vector3Int leftTilePosition = new Vector3Int(x - 1, y, 0);
        Vector3Int rightTilePosition = new Vector3Int(x + 1, y, 0);
        Vector3Int upperTilePosition = new Vector3Int(x, y + 1, 0);

        bool sameBotLeftTileExists = TileManager.Instance.WallTm.HasTile(leftTilePosition) &&
        (GameDataRegistry.Instance.GetTileIdFromTileBase(TileManager.Instance.WallTm.GetTile(leftTilePosition)) == GameDataRegistry.Instance.GetTileIdFromTileBase(TileManager.Instance.WallTm.GetTile(tilePosition)));

        bool sameBotRightTileExists = TileManager.Instance.WallTm.HasTile(rightTilePosition) &&
        (GameDataRegistry.Instance.GetTileIdFromTileBase(TileManager.Instance.WallTm.GetTile(rightTilePosition)) == GameDataRegistry.Instance.GetTileIdFromTileBase(TileManager.Instance.WallTm.GetTile(tilePosition)));

        if (!sameBotLeftTileExists && !sameBotRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Single);
        }
        else if (sameBotLeftTileExists && !sameBotRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Right);
        }
        else if (!sameBotLeftTileExists && sameBotRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Left);
        }
        else if (sameBotLeftTileExists && sameBotRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Center);
        }
    }

    private void SetUpperWallTile(Vector3Int upperTilePosition, TileDataSO tileAtPosition, TopTileType topTileType)
    {
        Vector3Int baseTilePosition = upperTilePosition + Vector3Int.down;

        if (_upperWallTm.HasTile(baseTilePosition))
        {
            Vector3Int lowerTilePosition = baseTilePosition + Vector3Int.down;
            int lowerTileId = GameDataRegistry.Instance.GetTileIdFromTileBase(TileManager.Instance.WallTm.GetTile(lowerTilePosition));
            int baseTileId = GameDataRegistry.Instance.GetTileIdFromTileBase(TileManager.Instance.WallTm.GetTile(baseTilePosition));

            if (baseTileId == lowerTileId)
            {
                _upperWallTm.SetTile(baseTilePosition, null);
                _upperOreWallTm.SetTile(baseTilePosition, null);
            }
        }
    
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        switch (topTileType)
        {
            case TopTileType.Single:
                tile.sprite = tileAtPosition.TopTileSingle;
                break;
            case TopTileType.Right:
                tile.sprite = tileAtPosition.TopTileRight;
                break;
            case TopTileType.Left:
                tile.sprite = tileAtPosition.TopTileLeft;
                break;
            case TopTileType.Center:
                tile.sprite = tileAtPosition.TopTileCenter;
                break;
        }
        
        _upperWallTm.SetTile(upperTilePosition, tile);
    }

    private void OnDestroy()
    {
        GameWorld.Instance.OnBiomeTransitionEnd -= RefreshUpperWallTiles;
    }
}
