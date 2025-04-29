using UnityEngine;

public class TopTile : MonoBehaviour
{
    private SpriteRenderer _tileSpriteRenderer;
    private TileSO _wallSO;
    private Vector3Int _botMiddleTilePosition;
    private TileType _tileType;

    private void Awake()
    {
        _tileSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        Physics2D.queriesHitTriggers = true;
    }
    
    public void Initialize(TileSO wallSO, Vector3Int botMiddleTilePosition)
    {
        _wallSO = wallSO;
        _botMiddleTilePosition = botMiddleTilePosition;
        _tileType = wallSO.TileType;

        UpdateSelf();
        UpdateSortingLayer();
        TileManager.Instance.UpdateNearbyTopTiles(_botMiddleTilePosition);
    }
    
    public void UpdateSelf()
    {
        Vector3Int botLeftTilePosition = _botMiddleTilePosition + Vector3Int.left;
        Vector3Int botRightTilePosition = _botMiddleTilePosition + Vector3Int.right;
        
        bool botLeftTileExists = TileManager.Instance.HasTile(botLeftTilePosition, _tileType);
        bool botRightTileExists = TileManager.Instance.HasTile(botRightTilePosition, _tileType);
        bool botMiddleTileExists = TileManager.Instance.HasTile(_botMiddleTilePosition, _tileType);
        bool hasSpaceForSelf = !TileManager.Instance.HasTile(_botMiddleTilePosition + Vector3Int.up, _tileType);
        
        if(!botMiddleTileExists || !hasSpaceForSelf)
        {
            Destroy(gameObject);
        }
        else if(!botLeftTileExists && !botRightTileExists && botMiddleTileExists)
        {
            _tileSpriteRenderer.sprite = _wallSO.TopTileSingle;
        }
        else if(botLeftTileExists && !botRightTileExists && botMiddleTileExists)
        {
            _tileSpriteRenderer.sprite = _wallSO.TopTileRight;
        }
        else if(!botLeftTileExists && botRightTileExists && botMiddleTileExists)
        {
            _tileSpriteRenderer.sprite = _wallSO.TopTileLeft;
        }
        else if(botLeftTileExists && botRightTileExists && botMiddleTileExists)
        {
            _tileSpriteRenderer.sprite = _wallSO.TopTileCenter;
        }

        UpdateSortingLayer();
    }

    private void UpdateSortingLayer()
    {
        switch (_tileType)
        {
            case TileType.Wall:
                _tileSpriteRenderer.sortingOrder = 0;
                break;
            case TileType.Ore:
                _tileSpriteRenderer.sortingOrder = 1;
                break;
        }
    }
}
