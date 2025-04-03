using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BiomeFileData 
{
	[SerializeReference] public List<WorldObjectFileData> WorldObjectsList = new();
	[SerializeReference] public List<ChunkFileData> ChunksList = new();
	[SerializeReference] public List<ChestFileData> ChestList = new();
}

[Serializable]
public class ChestFileData
{
	public Vector2Int ChestPosition;
	public List<ItemFileData> ChestItems;
}

[Serializable]
public struct ItemFileData
{
	public int SlotIndex;
	public int ItemId; // Unique ID of the item
	public int Quantity; // Quantity of the item
	public List<int> MagicArray;
}

[Serializable]
public class TileFileData // For Serialization
{
	public int TileId;
	public Vector2Int Pos; // Could be position in chunk or in the world not sure yet
	public TileType TileType;
}

[Serializable]
public class ChunkFileData // For Serialization
{
	public Vector2Int ChunkPosition;
	public List<TileFileData> GroundTiles;
	public List<TileFileData> FloorTiles;
	public List<TileFileData> WallTiles;
	public List<TileFileData> OreTiles;
	public int Size; // Width and Height
}

[Serializable]
public class WorldObjectFileData // For Serialization
{
	public int WorldObjectId;
	public Vector2Int Pos;
	public CardinalDirection Orientation;
	
	public WorldObjectFileData(int id, Vector2Int pos, CardinalDirection orientation)
	{
		WorldObjectId = id;
		Pos = pos;
		Orientation = orientation;
	}
}

[Serializable]
public class DoorObjectFileData : WorldObjectFileData
{
	public bool IsOpen;

	public DoorObjectFileData(int worldObject, Vector2Int pos, CardinalDirection orientation, bool isOpen) : base(worldObject, pos, orientation)
	{
		IsOpen = isOpen;
	}
}