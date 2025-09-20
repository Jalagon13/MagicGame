using System.Collections.Generic;
using UnityEngine;

public class OreGenerationStep : GenerationStep
{
    [Header("Ore Blob Generation")]
    [SerializeField] 
    private int _blobCount = 15, _blobSizeMin = 5, _blobSizeMax = 10;
    
    [SerializeField] 
    private float _minBlobSpacing = 12f;

    public override void Execute(BaseGenerationData genData)
    {
        if (genData is not CaveGenerationData caveGenData) return;
        int side = ChunkManager.BIOME_SIDE_LENGTH;
        bool[,] oreMask = new bool[side, side];

        // Poisson disk sampling for blob centers
        List<Vector2> blobCenters = new();
        int maxAttempts = _blobCount * 10;
        int attempts = 0;
        System.Random rng = new System.Random((int)System.DateTime.Now.Ticks);
        while (blobCenters.Count < _blobCount && attempts < maxAttempts)
        {
            float cx = (float)rng.NextDouble() * (side - 1);
            float cy = (float)rng.NextDouble() * (side - 1);
            Vector2 candidate = new Vector2(cx, cy);
            bool valid = true;
            foreach (var center in blobCenters)
            {
                if (Vector2.Distance(center, candidate) < _minBlobSpacing)
                {
                    valid = false;
                    break;
                }
            }
            if (valid)
            {
                blobCenters.Add(candidate);
            }
            attempts++;
        }

        // For each blob center, generate a compact blob using a center-biased constrained random walk
        foreach (var center in blobCenters)
        {
            int blobSize = rng.Next(_blobSizeMin, _blobSizeMax + 1);
            int cx = Mathf.RoundToInt(center.x);
            int cy = Mathf.RoundToInt(center.y);

            HashSet<Vector2Int> placedTiles = new HashSet<Vector2Int>();
            List<Vector2Int> placedList = new List<Vector2Int>();

            Vector2Int centerTile = new Vector2Int(cx, cy);
            if (cx >= 0 && cx < side && cy >= 0 && cy < side)
            {
                oreMask[cx, cy] = true;
                placedTiles.Add(centerTile);
                placedList.Add(centerTile);
            }

            while (placedTiles.Count < blobSize && placedList.Count > 0)
            {
                // Center-biased selection: higher probability for tiles closer to center
                // Compute weights inversely proportional to distance squared (plus small epsilon)
                float totalWeight = 0f;
                List<float> weights = new List<float>(placedList.Count);
                for (int i = 0; i < placedList.Count; i++)
                {
                    float distSq = (placedList[i].x - cx) * (placedList[i].x - cx) + (placedList[i].y - cy) * (placedList[i].y - cy) + 0.1f;
                    float w = 1f / distSq;
                    weights.Add(w);
                    totalWeight += w;
                }
                // Pick a tile index using weighted random selection
                float pick = (float)rng.NextDouble() * totalWeight;
                int chosenIdx = 0;
                float accum = 0f;
                for (int i = 0; i < weights.Count; i++)
                {
                    accum += weights[i];
                    if (pick <= accum)
                    {
                        chosenIdx = i;
                        break;
                    }
                }
                Vector2Int chosenTile = placedList[chosenIdx];

                // Pick random neighbor (up/down/left/right)
                int[][] neighbors = new int[][] { new int[] {1,0}, new int[] {-1,0}, new int[] {0,1}, new int[] {0,-1} };
                int[] dir = neighbors[rng.Next(0, neighbors.Length)];
                int nx = chosenTile.x + dir[0];
                int ny = chosenTile.y + dir[1];
                if (nx >= 0 && nx < side && ny >= 0 && ny < side)
                {
                    Vector2Int npos = new Vector2Int(nx, ny);
                    if (!placedTiles.Contains(npos))
                    {
                        oreMask[nx, ny] = true;
                        placedTiles.Add(npos);
                        placedList.Add(npos);
                    }
                }
            }
        }

        // Place ore tiles according to oreMask
        for (int x = 0; x < side; x++)
        {
            for (int y = 0; y < side; y++)
            {
                if (caveGenData.MostFrontRenderedTileMatrix[x, y] == GameDataRegistry.Instance.GetTileIdFromTileData(caveGenData.StoneWallTile))
                {
                    if (oreMask[x, y])
                    {
                        caveGenData.SetTileData(x, y, caveGenData.SilverOreTile);
                    }
                }
            }
        }

        oreMask = null;
    }
}