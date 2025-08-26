using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BiomeFileData 
{
	[SerializeReference] 
	public List<ResourceObjectFileData> ResourceObjectsList = new();
	[SerializeReference] 
	public List<ChunkFileData> ChunksList = new();
	[SerializeReference] 
	public List<ChestFileData> ChestList = new();
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
	public int SelectedSpellIndex; // For wands, the index of the selected spell
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
	public List<TileFileData> FoliageTiles;
	public List<TileFileData> LiquidTiles;
	public int Size; // Width and Height
}

[Serializable]
public class ResourceObjectFileData // For Serialization
{
	public ushort Id;
	public Vector2Int Pos;
	public CardinalDirection Orientation;
	
	public ResourceObjectFileData(ushort id, Vector2Int pos, CardinalDirection orientation)
	{
		Id = id;
		Pos = pos;
		Orientation = orientation;
	}
}

[Serializable]
public class DoorObjectFileData : ResourceObjectFileData
{
	public bool IsOpen;

	public DoorObjectFileData(ushort resourceObject, Vector2Int pos, CardinalDirection orientation, bool isOpen) : base(resourceObject, pos, orientation)
	{
		IsOpen = isOpen;
	}
}