using System.Collections.Generic;
using UnityEngine;

public static class DirectionsHelper
{
    public static readonly List<Vector2Int> DirectionOffsets4 = new List<Vector2Int>
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1)
    };

    //Create directions list for 8 directions
    public static readonly List<Vector2Int> DirectionOffsets8 = new List<Vector2Int>
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
        new Vector2Int(1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1)
    };
}
