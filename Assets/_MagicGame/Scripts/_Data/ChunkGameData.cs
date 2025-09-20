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
	private readonly List<TileGameData> _foliageTileGameDataList;
	private readonly List<ResourceObjectGameData> _worldObjectGameDataList;
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
		_foliageTileGameDataList = new List<TileGameData>(chunkSize * chunkSize);
		_worldObjectGameDataList = new List<ResourceObjectGameData>(chunkSize * chunkSize);

		_tileTypeToList = new Dictionary<TileType, List<TileGameData>>
		{
			{ TileType.Terrain, _terrainTileGameDataList },
			{ TileType.Floor, _floorTileGameDataList },
			{ TileType.Wall, _wallTileGameDataList },
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
	public void RemoveTileData(Vector2Int position, TileType tileType)
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
	public void AddTileData(Vector2Int position, TileDataSO tile)
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
	
	public bool HasWorldObject(Vector2Int position)
	{
		return _worldObjectGameDataList.Exists(o => o.Position == position);
	}

	public List<ResourceObjectGameData> GetWorldObjects()
	{
		return _worldObjectGameDataList;
	}

	public void DeserializeObjectData(ResourceObjectFileData worldObjectFileData, ResourceObject worldObject, CardinalDirection orientation) // For deserialization
	{
		ResourceObjectGameData worldObjectToAdd;
		
		if(worldObjectFileData is DoorObjectFileData doorObjectFileData)
		{
			worldObjectToAdd = new DoorObjectGameData(worldObject, worldObjectFileData.Pos, orientation, doorObjectFileData.IsOpen);
		}
		else
		{
			worldObjectToAdd = new ResourceObjectGameData(worldObject, worldObjectFileData.Pos, orientation);
		}
		
		_worldObjectGameDataList.Add(worldObjectToAdd);
	}

	public void AddObjectData(Vector2Int position, ResourceObject worldObject, CardinalDirection orientation) // For run time game play
	{
		ResourceObjectGameData worldObjectToAdd;

		if (worldObject is DoorObject)
		{
			worldObjectToAdd = new DoorObjectGameData(worldObject, position, orientation, false);
		}
		else
		{
			worldObjectToAdd = new ResourceObjectGameData(worldObject, position, orientation);
		}

		for (int i = 0; i < _worldObjectGameDataList.Count; i++)
		{
			if (_worldObjectGameDataList[i].Position == position)
			{
				// Found something already there
				Debug.LogWarning($"Found {_worldObjectGameDataList[i].Rsc} already there at {position}, replacing it with {worldObject}");
				_worldObjectGameDataList.RemoveAt(i);
				break;
			}
		}

		_worldObjectGameDataList.Add(worldObjectToAdd);
	}

	public void RemoveResourceData(Vector2Int position)
	{
		foreach (ResourceObjectGameData assetGameData in _worldObjectGameDataList)
		{
			if(assetGameData.Position == position)
			{
				_worldObjectGameDataList.Remove(assetGameData);
				return;
			}
		}
	}
}

public class ResourceObjectGameData
{
	public ResourceObject Rsc { get; private set; }
	public Vector2Int Position { get; private set; }
	public CardinalDirection Orientation { get; set; }
	
	public ResourceObjectGameData(ResourceObject resourceObject, Vector2Int position, CardinalDirection orientation)
	{
		Rsc = resourceObject;
		Position = position;
		Orientation = orientation;
	}
}

public class DoorObjectGameData : ResourceObjectGameData
{
	public bool IsOpen { get; private set; }

	public DoorObjectGameData(ResourceObject worldObject, Vector2Int position, CardinalDirection orientation, bool isOpen) : base(worldObject, position, orientation)
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
	public TileDataSO TileSO { get; private set; }
	public Vector2Int TilePosition { get; private set; }

	public TileGameData(TileDataSO tileBase, Vector2Int position)
	{
		TileSO = tileBase;
		TilePosition = position;
	}
}