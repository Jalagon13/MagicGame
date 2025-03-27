using UnityEngine;

public class TopTile : MonoBehaviour
{
    private SpriteRenderer _tileSpriteRenderer;
    private TileSO _wallSO;
    private Vector3Int _botMiddleTilePosition;

    private void Awake()
    {
        _tileSpriteRenderer = transform.GetChild(0).GetComponent<SpriteRenderer>();
        Physics2D.queriesHitTriggers = true;
    }
    
    public void Initialize(TileSO wallSO, Vector3Int botMiddleTilePosition)
    {
        _wallSO = wallSO;
        _botMiddleTilePosition = botMiddleTilePosition;

        UpdateSelf();

        // After self update, update other top tiles in area
        Vector2 searchPosition = new Vector2(_botMiddleTilePosition.x + 0.5f, _botMiddleTilePosition.y + 1f);
        Collider2D[] colliders = Physics2D.OverlapCircleAll(searchPosition, 3f);

        foreach (var collider in colliders)
        {
            TopTile topTileFound = collider.GetComponent<TopTile>();
            if (topTileFound != null)
            {
                topTileFound.UpdateSelf();
            }
        }
    }
    
    public void UpdateSelf()
    {
        Vector3Int botLeftTilePosition = _botMiddleTilePosition + Vector3Int.left;
        Vector3Int botRightTilePosition = _botMiddleTilePosition + Vector3Int.right;
        
        bool botLeftTileExists = TileManager.Instance.HasTile(botLeftTilePosition, TileType.Wall);
        bool botRightTileExists = TileManager.Instance.HasTile(botRightTilePosition, TileType.Wall);
        bool botMiddleTileExists = TileManager.Instance.HasTile(_botMiddleTilePosition, TileType.Wall);
        bool hasSpaceForSelf = !TileManager.Instance.HasTile(_botMiddleTilePosition + Vector3Int.up, TileType.Wall);
        
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
    }
}
