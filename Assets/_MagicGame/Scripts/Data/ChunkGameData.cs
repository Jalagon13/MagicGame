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
	public List<TileGameData> WallTileGameDataList;
	public List<TileGameData> FloorTileGameDataList;
	public List<WorldObjectGameData> WorldObjectGameDataList;
	public int Size { get; private set; }

	public ChunkGameData(int chunkSize, Vector2Int chunkPosition)
	{
		Size = chunkSize;
		ChunkPosition = chunkPosition;
		GroundTileGameDataList = new();
		WallTileGameDataList = new();
		WorldObjectGameDataList = new();
		FloorTileGameDataList = new();
	}
	
	// When a tile is destroyed, delete the tile data in chunk
	public void RemoveTileDataIfExists(Vector2Int position, TileType tileType)
	{
		if(tileType == TileType.Wall)
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
		
		if(tile.TileType == TileType.Wall)
		{
			WallTileGameDataList.Add(tileToAdd);
		}
		else if(tile.TileType == TileType.Floor)
		{
			Debug.Log($"Floor tile added: {tileToAdd.TilePosition}");
			FloorTileGameDataList.Add(tileToAdd);
			Debug.Log($"Count of floor tiles: {FloorTileGameDataList.Count}");
		}
	}
	
	public void AddObjectData(WorldObjectFileData worldObjectFileData, WorldObject worldObject) // For deserialization
	{
		WorldObjectGameData worldObjectToAdd;
		
		if(worldObjectFileData is DoorObjectFileData doorObjectFileData)
		{
			worldObjectToAdd = new DoorObjectGameData(worldObject, worldObjectFileData.Pos, doorObjectFileData.IsOpen);
		}
		else
		{
			worldObjectToAdd = new WorldObjectGameData(worldObject, worldObjectFileData.Pos);
		}
		
		WorldObjectGameDataList.Add(worldObjectToAdd);
	}

	public void AddObjectData(Vector2Int position, WorldObject worldObject) // For run time game play
	{
		WorldObjectGameData worldObjectToAdd;

		if (worldObject is DoorObject)
		{
			worldObjectToAdd = new DoorObjectGameData(worldObject, position, false);
		}
		else
		{
			worldObjectToAdd = new WorldObjectGameData(worldObject, position);
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
	
	public WorldObjectGameData(WorldObject worldObject, Vector2Int position)
	{
		WO = worldObject;
		Position = position;
	}
}

public class DoorObjectGameData : WorldObjectGameData
{
	public bool IsOpen { get; private set; }

	public DoorObjectGameData(WorldObject worldObject, Vector2Int position, bool isOpen) : base(worldObject, position)
	{
		IsOpen = isOpen;
	}
	
	public void ToggleDoor()
	{
		IsOpen = !IsOpen;
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