using System.Collections.Generic;
using UnityEngine;

public static class GenerationUtils
{
    public static HashSet<Vector2Int> GetEdgeTiles(List<TileDataSO> tilesToSearchFor, List<TileDataSO> tilesAdjacentTo, List<Vector2Int> directions, int[,] tileMatrix)
    {
        HashSet<Vector2Int> edgeTiles = new HashSet<Vector2Int>();

        int width = tileMatrix.GetLength(0);
        int height = tileMatrix.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int tileId = tileMatrix[x, y];
                TileDataSO currentTile = GameManager.Instance.GetTileSOFromID(tileId);

                if (tilesToSearchFor.Contains(currentTile) && IsAdjacentToTiles(x, y, tilesAdjacentTo, directions, tileMatrix))
                {
                    edgeTiles.Add(new Vector2Int(x, y));
                }
            }
        }

        return edgeTiles;
    }

    public static bool IsAdjacentToTiles(int x, int y, List<TileDataSO> tilesToCheckFor, List<Vector2Int> directions, int[,] tileMatrix)
    {
        int width = tileMatrix.GetLength(0);
        int height = tileMatrix.GetLength(1);

        foreach (Vector2Int direction in directions)
        {
            int nx = x + direction.x;
            int ny = y + direction.y;

            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                int neighborTileId = tileMatrix[nx, ny];
                TileDataSO neighborTile = GameManager.Instance.GetTileSOFromID(neighborTileId);

                if (tilesToCheckFor.Contains(neighborTile))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static HashSet<Vector2Int> ExpandEdgeTiles(HashSet<Vector2Int> initialShoreTiles, int width, List<TileDataSO> validTiles, int[,] tileMatrix)
    {
        HashSet<Vector2Int> expandedEdgeTiles = new HashSet<Vector2Int>();
        HashSet<Vector2Int> currentLayerTiles = new HashSet<Vector2Int>(initialShoreTiles);

        int matrixWidth = tileMatrix.GetLength(0);
        int matrixHeight = tileMatrix.GetLength(1);

        for (int i = 0; i < width; i++)
        {
            HashSet<Vector2Int> nextLayerTiles = new HashSet<Vector2Int>();

            foreach (Vector2Int tile in currentLayerTiles)
            {
                foreach (Vector2Int direction in DirectionsHelper.DirectionOffsets8)
                {
                    Vector2Int adjacentTile = tile + direction;

                    if (adjacentTile.x >= 0 && adjacentTile.x < matrixWidth && adjacentTile.y >= 0 && adjacentTile.y < matrixHeight)
                    {
                        int tileId = tileMatrix[adjacentTile.x, adjacentTile.y];
                        TileDataSO tileSO = GameManager.Instance.GetTileSOFromID(tileId);

                        if (validTiles.Contains(tileSO) && !expandedEdgeTiles.Contains(adjacentTile))
                        {
                            nextLayerTiles.Add(adjacentTile);
                            expandedEdgeTiles.Add(adjacentTile);
                        }
                    }
                }
            }

            currentLayerTiles = nextLayerTiles;
        }

        return expandedEdgeTiles;
    }
}
