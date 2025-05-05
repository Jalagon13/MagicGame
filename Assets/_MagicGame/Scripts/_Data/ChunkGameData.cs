using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Class to store chunk data including tiles and world assets
public class ChunkGameData
{
	public Vector2Int ChunkPosition { get; private set; }
	public List<TileGameData> GroundTileGameDataList;
	public List<TileGameData> FloorTileGameDataList;
	public List<TileGameData> WallTileGameDataList;
	public List<TileGameData> OreTileGameDataList;
	public List<TileGameData> FoliageTileGameDataList;
	public List<TileGameData> LiquidTileGameDataList;
	public List<WorldObjectGameData> WorldObjectGameDataList;
	public int Size { get; private set; }

	private readonly Dictionary<TileType, List<TileGameData>> _tileTypeToList;

	public ChunkGameData(int chunkSize, Vector2Int chunkPosition)
	{
		Size = chunkSize;
		ChunkPosition = chunkPosition;
		GroundTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		FloorTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		WallTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		OreTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		FoliageTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		LiquidTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		WorldObjectGameDataList = new List<WorldObjectGameData>(chunkSize * chunkSize);

		_tileTypeToList = new Dictionary<TileType, List<TileGameData>>
		{
			{ TileType.Ground, GroundTileGameDataList },
			{ TileType.Floor, FloorTileGameDataList },
			{ TileType.Wall, WallTileGameDataList },
			{ TileType.Foliage, FoliageTileGameDataList },
			{ TileType.Liquid, LiquidTileGameDataList }
		};
	}
	
	// When a tile is destroyed, delete the tile data in chunk
	public void RemoveTileDataIfExists(Vector2Int position, TileType tileType)
	{
		if(tileType == TileType.Ore)
		{
			foreach (TileGameData tile in WallTileGameDataList)
			{
				// If position is found
				if (tile.TilePosition == position)
				{
					// Delete data and return
					WallTileGameDataList.Remove(tile);
					break;
				}
			}
			
			foreach (TileGameData tile in OreTileGameDataList)
			{
				// If position is found
				if (tile.TilePosition == position)
				{
					// Delete data and return
					OreTileGameDataList.Remove(tile);
					return;
				}
			}
		}
		else if(tileType == TileType.Wall)
		{
			foreach (TileGameData tile in WallTileGameDataList)
			{
				// If position is found
				if(tile.TilePosition == position)
				{
					// Delete data and return
					WallTileGameDataList.Remove(tile);
					return;
				}
			}
		}
		else
		{
			foreach (TileGameData tile in FloorTileGameDataList)
			{
				// If position is found
				if(tile.TilePosition == position)
				{
					// Delete data and return
					FloorTileGameDataList.Remove(tile);
					return;
				}
			}
		}
	}
	
	// When a tile is placed, add tile data in chunk
	public void AddTileData(Vector2Int position, TileSO tile)
	{
		TileGameData tileToAdd = new(tile, position);

		if (_tileTypeToList.TryGetValue(tile.TileType, out var list))
		{
			int existingIndex = list.FindIndex(t => t.TilePosition == position);
			if (existingIndex >= 0)
				list[existingIndex] = tileToAdd;
			else
				list.Add(tileToAdd);
		}
	}
	
	public void DeserializeObjectData(WorldObjectFileData worldObjectFileData, WorldObject worldObject, CardinalDirection orientation) // For deserialization
	{
		WorldObjectGameData worldObjectToAdd;
		
		if(worldObjectFileData is DoorObjectFileData doorObjectFileData)
		{
			worldObjectToAdd = new DoorObjectGameData(worldObject, worldObjectFileData.Pos, orientation, doorObjectFileData.IsOpen);
		}
		else
		{
			worldObjectToAdd = new WorldObjectGameData(worldObject, worldObjectFileData.Pos, orientation);
		}
		
		WorldObjectGameDataList.Add(worldObjectToAdd);
	}

	public void AddObjectData(Vector2Int position, WorldObject worldObject, CardinalDirection orientation) // For run time game play
	{
		WorldObjectGameData worldObjectToAdd;

		if (worldObject is DoorObject)
		{
			worldObjectToAdd = new DoorObjectGameData(worldObject, position, orientation, false);
		}
		else
		{
			worldObjectToAdd = new WorldObjectGameData(worldObject, position, orientation);
		}

		for (int i = 0; i < WorldObjectGameDataList.Count; i++)
		{
			if (WorldObjectGameDataList[i].Position == position)
			{
				// Found something already there
				Debug.LogWarning($"Found {WorldObjectGameDataList[i].WO} already there at {position}, replacing it with {worldObject}");
				WorldObjectGameDataList.RemoveAt(i);
				break;
			}
		}

		WorldObjectGameDataList.Add(worldObjectToAdd);
	}

	public void RemoveObjectData(Vector2Int position)
	{
		foreach (WorldObjectGameData assetGameData in WorldObjectGameDataList)
		{
			if(assetGameData.Position == position)
			{
				WorldObjectGameDataList.Remove(assetGameData);
				return;
			}
		}
	}
}

public class WorldObjectGameData
{
	public WorldObject WO { get; private set; }
	public Vector2Int Position { get; private set; }
	public CardinalDirection Orientation { get; set; }
	
	public WorldObjectGameData(WorldObject worldObject, Vector2Int position, CardinalDirection orientation)
	{
		WO = worldObject;
		Position = position;
		Orientation = orientation;
	}
}

public class DoorObjectGameData : WorldObjectGameData
{
	public bool IsOpen { get; private set; }

	public DoorObjectGameData(WorldObject worldObject, Vector2Int position, CardinalDirection orientation, bool isOpen) : base(worldObject, position, orientation)
	{
		IsOpen = isOpen;
		Orientation = orientation;
	}
	
	public void SetDoorState(bool isOpen)
	{
		IsOpen = isOpen;
	}
}

// Data used in the game to store tile information (versus tile information stored on file)
public struct TileGameData
{
	public TileSO TileSO { get; private set; }
	public Vector2Int TilePosition { get; private set; }

	public TileGameData(TileSO tileBase, Vector2Int position)
	{
		TileSO = tileBase;
		TilePosition = position;
	}
}