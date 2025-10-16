using System.Collections.Generic;
using UnityEngine;

namespace ProjectTinker
{
    public static class PoissonDiskSampling
    {
        public static HashSet<Vector2Int> GeneratePoints(NoiseMapSO noiseMapSO, float minRadius, float maxRadius)
        {
            int width = noiseMapSO.NoiseTexture.width;
            int height = noiseMapSO.NoiseTexture.height;
            int maxPoints = width * height;

            float cellSize = minRadius / Mathf.Sqrt(2);
            int gridWidth = Mathf.CeilToInt(width / cellSize);
            int gridHeight = Mathf.CeilToInt(height / cellSize);

            Vector2Int[,] grid = new Vector2Int[gridWidth, gridHeight];
            for (int i = 0; i < gridWidth; i++)
                for (int j = 0; j < gridHeight; j++)
                    grid[i, j] = new Vector2Int(-1, -1);

            HashSet<Vector2Int> points = new HashSet<Vector2Int>();
            List<Vector2> processList = new List<Vector2>();

            Vector2 startPoint = new Vector2(Random.Range(0, width), Random.Range(0, height));
            Vector2Int startInt = Vector2Int.RoundToInt(startPoint);
            processList.Add(startPoint);
            points.Add(startInt);

            int startGridX = (int)(startPoint.x / cellSize);
            int startGridY = (int)(startPoint.y / cellSize);
            grid[startGridX, startGridY] = startInt;

            while (processList.Count > 0 && points.Count < maxPoints)
            {
                int index = Random.Range(0, processList.Count);
                Vector2 point = processList[index];
                processList.RemoveAt(index);

                float noiseValue = noiseMapSO.NoiseTexture.GetPixel((int)point.x, (int)point.y).grayscale;
                float localRadius = Mathf.Lerp(maxRadius, minRadius, noiseValue);

                for (int i = 0; i < 30; i++)
                {
                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float radius = Random.Range(localRadius, 2 * localRadius);
                    Vector2 newPoint = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                    int x = Mathf.RoundToInt(newPoint.x);
                    int y = Mathf.RoundToInt(newPoint.y);

                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        Vector2Int candidate = new Vector2Int(x, y);
                        int gridX = (int)(x / cellSize);
                        int gridY = (int)(y / cellSize);

                        bool tooClose = false;
                        for (int gx = Mathf.Max(0, gridX - 2); gx <= Mathf.Min(gridWidth - 1, gridX + 2); gx++)
                        {
                            for (int gy = Mathf.Max(0, gridY - 2); gy <= Mathf.Min(gridHeight - 1, gridY + 2); gy++)
                            {
                                Vector2Int neighbor = grid[gx, gy];
                                if (neighbor.x != -1)
                                {
                                    if (Vector2Int.Distance(candidate, neighbor) < localRadius)
                                    {
                                        tooClose = true;
                                        break;
                                    }
                                }
                            }
                            if (tooClose) break;
                        }

                        if (!tooClose)
                        {
                            points.Add(candidate);
                            processList.Add(newPoint);
                            grid[gridX, gridY] = candidate;

                            if (points.Count >= maxPoints) break;
                        }
                    }
                }
            }

            return points;
        }


    }
}
