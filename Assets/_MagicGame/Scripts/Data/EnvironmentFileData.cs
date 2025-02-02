using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnvironmentFileData 
{
	public List<WorldObjectFileData> WorldObjectsList = new();
	public List<ChunkFileData> ChunksList = new();
	public List<ChestFileData> ChestList = new();
}

[System.Serializable]
public class ChestFileData
{
	public Vector2Int ChestPosition;
	public List<ItemFileData> ChestItems;
}

[System.Serializable]
public struct ItemFileData
{
	public int SlotIndex;
	public int ItemId; // Unique ID of the item
	public int Quantity; // Quantity of the item
}

[System.Serializable]
public class TileFileData // For Serialization
{
	public int TileId;
	public Vector2Int Pos; // Could be position in chunk or in the world not sure yet
	public TileType TileType;
}

[System.Serializable]
public class ChunkFileData // For Serialization
{
	public Vector2Int ChunkPosition;
	public List<TileFileData> GroundTiles;
	public List<TileFileData> WallTiles;
	public int Size; // Width and Height
}

[System.Serializable]
public class WorldObjectFileData // For Serialization
{
	public int WorldObjectId;
	public Vector2Int Pos;
	
	public WorldObjectFileData(int id, Vector2Int pos)
	{
		WorldObjectId = id;
		Pos = pos;
	}
}

[System.Serializable]
public class DoorObjectFileData : WorldObjectFileData
{
	public bool IsOpen;

	public DoorObjectFileData(int worldObject, Vector2Int pos, bool isOpen) : base(worldObject, pos)
	{
		IsOpen = isOpen;
	}
}