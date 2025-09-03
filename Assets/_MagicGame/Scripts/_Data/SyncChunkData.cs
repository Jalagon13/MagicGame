using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct SyncChunkData : IEquatable<SyncChunkData>, INetworkSerializable
{
	public Vector2Int SyncChunkPosition;
	
	public List<GenericGameObjectSyncData> SyncTerrainTileDataList;
	public List<GenericGameObjectSyncData> SyncFloorTileDataList;
	public List<GenericGameObjectSyncData> SyncWallTileDataList;
	public List<GenericGameObjectSyncData> SyncOreTileDataList;
	public List<GenericGameObjectSyncData> SyncLiquidTileDataList;
	public List<GenericGameObjectSyncData> SyncFoliageTileDataList;
	
	public List<ResourceSyncData> SyncObjectAssetDataList;
	public List<DoorObjectSyncData> SyncDoorObjectDataList;

	public bool Equals(SyncChunkData other)
	{
		return SyncChunkPosition == other.SyncChunkPosition &&
			   SyncTerrainTileDataList.Equals(other.SyncTerrainTileDataList) &&
			   SyncFloorTileDataList.Equals(other.SyncFloorTileDataList) &&
			   SyncWallTileDataList.Equals(other.SyncWallTileDataList) && 
			   SyncOreTileDataList.Equals(other.SyncOreTileDataList) &&
			   SyncLiquidTileDataList.Equals(other.SyncLiquidTileDataList) &&
			   SyncFoliageTileDataList.Equals(other.SyncFoliageTileDataList) &&
			   SyncObjectAssetDataList.Equals(other.SyncObjectAssetDataList) &&
			   SyncDoorObjectDataList.Equals(other.SyncDoorObjectDataList);
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref SyncChunkPosition);
		
		SerializeAgnosticDataList(serializer, ref SyncTerrainTileDataList);
		SerializeAgnosticDataList(serializer, ref SyncFloorTileDataList);
		SerializeAgnosticDataList(serializer, ref SyncWallTileDataList);
		SerializeAgnosticDataList(serializer, ref SyncOreTileDataList);
		SerializeAgnosticDataList(serializer, ref SyncLiquidTileDataList);
		SerializeAgnosticDataList(serializer, ref SyncFoliageTileDataList);
		SerializeWorldObjectDataList(serializer, ref SyncObjectAssetDataList);
		SerializeDoorDataList(serializer, ref SyncDoorObjectDataList);
	}
	
	private void SerializeWorldObjectDataList<T>(BufferSerializer<T> serializer, ref List<ResourceSyncData> worldObjectDataList) where T : IReaderWriter
	{
		if (serializer.IsWriter)
		{
			// Serialize the list length
			ushort listLength = (ushort)worldObjectDataList.Count;
			serializer.SerializeValue(ref listLength);

			// Serialize each world object in the list
			for (int i = 0; i < listLength; i++)
			{
				ResourceSyncData worldObjectData = worldObjectDataList[i];
				serializer.SerializeValue(ref worldObjectData);
			}
		}
		else
		{
			// Deserialize the list length first
			ushort listLength = 0;
			serializer.SerializeValue(ref listLength);

			// If the list length is 0, no further deserialization is needed
			if (listLength == 0)
			{
				return;
			}

			// Initialize or clear the list
			if (worldObjectDataList == null)
			{
				worldObjectDataList = new List<ResourceSyncData>();
			}
			else
			{
				worldObjectDataList.Clear();
			}

			// Deserialize each item in the list
			for (int i = 0; i < listLength; i++)
			{
				ResourceSyncData worldObjectData = default;
				serializer.SerializeValue(ref worldObjectData);
				worldObjectDataList.Add(worldObjectData);
			}
		}
	}
	
	private void SerializeDoorDataList<T>(BufferSerializer<T> serializer, ref List<DoorObjectSyncData> doorDataList) where T : IReaderWriter
	{
		if (serializer.IsWriter)
		{
			// Serialize the list length
			ushort listLength = (ushort)doorDataList.Count;
			serializer.SerializeValue(ref listLength);

			// Serialize each door in the list
			for (int i = 0; i < listLength; i++)
			{
				DoorObjectSyncData doorData = doorDataList[i];
				serializer.SerializeValue(ref doorData);
			}
		}
		else
		{
			// Deserialize the list length first
			ushort listLength = 0;
			serializer.SerializeValue(ref listLength);

			// If the list length is 0, no further deserialization is needed
			if (listLength == 0)
			{
				return;
			}

			// Initialize or clear the list
			if (doorDataList == null)
			{
				doorDataList = new List<DoorObjectSyncData>(listLength);
			}
			else
			{
				doorDataList.Clear();
			}

			// Deserialize each item in the list
			for (int i = 0; i < listLength; i++)
			{
				DoorObjectSyncData doorData = default;
				serializer.SerializeValue(ref doorData);
				doorDataList.Add(doorData);
			}
		}
	}
	
	private void SerializeAgnosticDataList<T>(BufferSerializer<T> serializer, ref List<GenericGameObjectSyncData> tileDataList) where T : IReaderWriter
	{
		if (serializer.IsWriter)
		{
			// Serialize the list length
			ushort listLength = (ushort)tileDataList.Count;
			serializer.SerializeValue(ref listLength);

			// Serialize each tile in the list
			for (int i = 0; i < listLength; i++)
			{
				GenericGameObjectSyncData syncTileData = tileDataList[i];
				serializer.SerializeValue(ref syncTileData);
			}
		}
		else
		{
			// Deserialize the list length first
			ushort listLength = 0;
			serializer.SerializeValue(ref listLength);

			// If the list length is 0, no further deserialization is needed
			if (listLength == 0)
			{
				return; // Skip deserialization if the list length is 0
			}

			// Initialize the list if it's null
			if (tileDataList == null)
			{
				tileDataList = new List<GenericGameObjectSyncData>(listLength);
			}
			else
			{
				tileDataList.Clear(); // Clear the list if it's already initialized
			}

			// Deserialize each item in the list
			for (int i = 0; i < listLength; i++)
			{
				GenericGameObjectSyncData syncTileData = default;
				serializer.SerializeValue(ref syncTileData);
				tileDataList.Add(syncTileData);
			}
		}
	}
}

// For any syncing that just needs pos and id.
public struct GenericGameObjectSyncData : IEquatable<GenericGameObjectSyncData>, INetworkSerializable
{
	public Vector2Int Position;
	public ushort ID;

	public bool Equals(GenericGameObjectSyncData other)
	{
		return Position.Equals(other.Position) && ID == other.ID;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref Position);
		serializer.SerializeValue(ref ID);
	}
}

public struct ResourceSyncData : IEquatable<ResourceSyncData>, INetworkSerializable
{
	public Vector2Int Position;
	public ushort ID;
	public CardinalDirection Orientation;

	public bool Equals(ResourceSyncData other)
	{
		return Position.Equals(other.Position) && ID == other.ID && Orientation == other.Orientation;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref Position);
		serializer.SerializeValue(ref ID);
		serializer.SerializeValue(ref Orientation);		
	}
}

// For Door data syncing
public struct DoorObjectSyncData : IEquatable<DoorObjectSyncData>, INetworkSerializable
{
	public Vector2Int Position;
	public ushort ID;
	public bool IsOpen;
	public CardinalDirection Orientation;
	
	public bool Equals(DoorObjectSyncData other)
	{
		return Position.Equals(other.Position) && IsOpen == other.IsOpen && ID == other.ID && Orientation == other.Orientation;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref Position);
		serializer.SerializeValue(ref ID);
		serializer.SerializeValue(ref IsOpen);
		serializer.SerializeValue(ref Orientation);
	}
}
