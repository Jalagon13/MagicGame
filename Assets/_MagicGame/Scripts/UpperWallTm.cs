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
        _upperOreWallTm = transform.GetChild(0).GetComponent<Tilemap>();
    }

    private void Start()
    {
        WorldManager.Instance.OnBiomeTransitionEnd += RefreshUpperWallTiles;
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
                if (TileRenderManager.Instance.WallTm.HasTile(neighborPos))
                {
                    TryToRenderUpperWallTile(neighborPos.x, neighborPos.y);
                }
            }
        }
    }

    public void ClearAllTopTiles()
    {
        _upperWallTm.ClearAllTiles();
    }

    private void RefreshUpperWallTiles(object sender, EventArgs e)
    {
        for (int x = 0; x < ChunkManager.BIOME_SIDE_LENGTH; x++)
        {
            for (int y = 0; y < ChunkManager.BIOME_SIDE_LENGTH; y++)
            {
                if(TileRenderManager.Instance.WallTm.HasTile(new Vector3Int(x, y, 0)))
                {
                    TryToRenderUpperWallTile(x, y);
                }
            }
        }
        Debug.Log($"Refreshed upper wall tiles");
    }

    private void TryToRenderUpperWallTile(int x, int y)
    {
        TileSO tileAtPosition = GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.WallTm.GetTile(new Vector3Int(x, y, 0)));
        bool tileExistsAbove = TileRenderManager.Instance.WallTm.HasTile(new Vector3Int(x, y + 1, 0));
        
        if(tileExistsAbove)
        {
            TileSO aboveTileSO = GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.WallTm.GetTile(new Vector3Int(x, y + 1, 0)));  
            
            if(GameManager.Instance.GetTileIdFromTileSO(tileAtPosition) != GameManager.Instance.GetTileIdFromTileSO(aboveTileSO))
            {
                RenderUpperWallTile(tileAtPosition, x, y);
            }
        }
        else
        {
            RenderUpperWallTile(tileAtPosition, x, y);
        }
    }

    private void RenderUpperWallTile(TileSO tileAtPosition, int x, int y)
    {
        Vector3Int tilePosition = new Vector3Int(x, y, 0);
        Vector3Int leftTilePosition = new Vector3Int(x - 1, y, 0);
        Vector3Int rightTilePosition = new Vector3Int(x + 1, y, 0);
        Vector3Int upperTilePosition = new Vector3Int(x, y + 1, 0);

        bool botLeftTileExists = TileRenderManager.Instance.WallTm.HasTile(leftTilePosition) &&
        (GameManager.Instance.GetTileIdFromTileBase(TileRenderManager.Instance.WallTm.GetTile(leftTilePosition)) == GameManager.Instance.GetTileIdFromTileBase(TileRenderManager.Instance.WallTm.GetTile(tilePosition)));

        bool botRightTileExists = TileRenderManager.Instance.WallTm.HasTile(rightTilePosition) &&
        (GameManager.Instance.GetTileIdFromTileBase(TileRenderManager.Instance.WallTm.GetTile(rightTilePosition)) == GameManager.Instance.GetTileIdFromTileBase(TileRenderManager.Instance.WallTm.GetTile(tilePosition)));

        if (!botLeftTileExists && !botRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Single);
        }
        else if (botLeftTileExists && !botRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Right);
        }
        else if (!botLeftTileExists && botRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Left);
        }
        else if (botLeftTileExists && botRightTileExists)
        {
            SetUpperWallTile(upperTilePosition, tileAtPosition, TopTileType.Center);
        }
    }

    private void SetUpperWallTile(Vector3Int upperTilePosition, TileSO tileAtPosition, TopTileType topTileType)
    {
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

        Vector3Int oreTilePosition = new Vector3Int(upperTilePosition.x, upperTilePosition.y - 1, 0);
        if (TileRenderManager.Instance.OreTm.HasTile(oreTilePosition))
        {
            TileBase oreBaseTile = TileRenderManager.Instance.OreTm.GetTile(oreTilePosition);
            TileSO oreTileSO = GameManager.Instance.GetTileSOFromTileBase(oreBaseTile);

            Tile oreTile = ScriptableObject.CreateInstance<Tile>();
            switch (topTileType)
            {
                case TopTileType.Single:
                    oreTile.sprite = oreTileSO.TopTileSingle;
                    break;
                case TopTileType.Right:
                    oreTile.sprite = oreTileSO.TopTileRight;
                    break;
                case TopTileType.Left:
                    oreTile.sprite = oreTileSO.TopTileLeft;
                    break;
                case TopTileType.Center:
                    oreTile.sprite = oreTileSO.TopTileCenter;
                    break;
            }
            _upperOreWallTm.SetTile(upperTilePosition, oreTile);
        }
    }

    private void OnDestroy()
    {
        WorldManager.Instance.OnBiomeTransitionEnd -= RefreshUpperWallTiles;
    }
}
