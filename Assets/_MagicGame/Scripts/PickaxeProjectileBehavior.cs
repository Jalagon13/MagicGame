using UnityEngine;

public class PickaxeProjectileBehavior : MonoBehaviour
{
    private void OnDestroy()
    {
        // Get the tilemap and player's environment
        var tilemapData = Environment.Instance.GetWallTilemapData();
        var playerEnvironment = Player.LocalClientInstance.GetPlayerEnvironment();

        // Get the tile position of the projectile
        Vector3Int minePos3D = tilemapData.GetTilemap().WorldToCell(transform.parent.transform.position);

        // Convert to Vector2Int
        Vector2Int minePos = new Vector2Int(minePos3D.x, minePos3D.y);

        // Mine the central tile
        tilemapData.HitTile(minePos, 35, playerEnvironment);

        // Define cardinal directions
        Vector2Int[] cardinalDirections =
        {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0)   // Right
        };

        foreach (Vector2Int direction in cardinalDirections)
        {
            Vector2Int adjacentTile = minePos + direction;
            tilemapData.HitTile(adjacentTile, 35, playerEnvironment);
        }
    }
}