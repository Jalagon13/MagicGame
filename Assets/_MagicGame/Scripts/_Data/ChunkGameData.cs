using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Class to store chunk data including tiles and world assets
public class ChunkGameData
{
	public Vector2Int ChunkPosition { get; private set; }
	private readonly List<TileGameData> _terrainTileGameDataList;
	private readonly List<TileGameData> _liquidTileGameDataList;
	private readonly List<TileGameData> _floorTileGameDataList;
	private readonly List<TileGameData> _wallTileGameDataList;
	private readonly List<TileGameData> _oreTileGameDataList;
	private readonly List<TileGameData> _foliageTileGameDataList;
	private readonly List<WorldObjectGameData> _worldObjectGameDataList;
	public int Size { get; private set; }

	private readonly Dictionary<TileType, List<TileGameData>> _tileTypeToList;

	public ChunkGameData(int chunkSize, Vector2Int chunkPosition)
	{
		Size = chunkSize;
		ChunkPosition = chunkPosition;
		_terrainTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		_liquidTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		_floorTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		_wallTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		_oreTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		_foliageTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		_worldObjectGameDataList = new List<WorldObjectGameData>(chunkSize * chunkSize);

		_tileTypeToList = new Dictionary<TileType, List<TileGameData>>
		{
			{ TileType.Terrain, _terrainTileGameDataList },
			{ TileType.Floor, _floorTileGameDataList },
			{ TileType.Wall, _wallTileGameDataList },
			{ TileType.Ore, _oreTileGameDataList },
			{ TileType.Liquid, _liquidTileGameDataList },
			{ TileType.Foliage, _foliageTileGameDataList },
		};
	}

	// Populate all tile lists after construction (e.g., during deserialization)
	public void DeserializeTileLists(
		List<TileGameData> terrainTiles,
		List<TileGameData> liquidTiles,
		List<TileGameData> floorTiles,
		List<TileGameData> wallTiles,
		List<TileGameData> oreTiles,
		List<TileGameData> foliageTiles)
	{
		var incomingData = new Dictionary<TileType, List<TileGameData>>
		{
			{ TileType.Terrain, terrainTiles },
			{ TileType.Liquid, liquidTiles },
			{ TileType.Floor, floorTiles },
			{ TileType.Wall, wallTiles },
			{ TileType.Ore, oreTiles },
			{ TileType.Foliage, foliageTiles },
		};

		foreach (var kvp in incomingData)
		{
			if (_tileTypeToList.TryGetValue(kvp.Key, out var targetList))
			{
				targetList.Clear();
				targetList.AddRange(kvp.Value);
			}
		}
	}

	public List<TileGameData> GetTileList(TileType type)
	{
		if (_tileTypeToList.TryGetValue(type, out var list))
			return list;

		Debug.LogWarning($"Tried to get tile list for unknown type {type}");
		return null;
	}

	// When a tile is destroyed, delete the tile data in chunk
	public void RemoveTileDataIfExists(Vector2Int position, TileType tileType)
	{
	    if (_tileTypeToList.TryGetValue(tileType, out var list))
	    {
	        int index = list.FindIndex(t => t.TilePosition == position);
	        if (index >= 0)
	        {
	            list.RemoveAt(index);
	            return;
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
			{
				list[existingIndex] = tileToAdd;
			}
			else
			{
				list.Add(tileToAdd);
			}
		}
	}

	public List<WorldObjectGameData> GetWorldObjects()
	{
		return _worldObjectGameDataList;
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
		
		_worldObjectGameDataList.Add(worldObjectToAdd);
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

		for (int i = 0; i < _worldObjectGameDataList.Count; i++)
		{
			if (_worldObjectGameDataList[i].Position == position)
			{
				// Found something already there
				Debug.LogWarning($"Found {_worldObjectGameDataList[i].WO} already there at {position}, replacing it with {worldObject}");
				_worldObjectGameDataList.RemoveAt(i);
				break;
			}
		}

		_worldObjectGameDataList.Add(worldObjectToAdd);
	}

	public void RemoveObjectData(Vector2Int position)
	{
		foreach (WorldObjectGameData assetGameData in _worldObjectGameDataList)
		{
			if(assetGameData.Position == position)
			{
				_worldObjectGameDataList.Remove(assetGameData);
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