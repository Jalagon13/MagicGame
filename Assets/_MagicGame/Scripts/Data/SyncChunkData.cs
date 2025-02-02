using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct SyncChunkData : IEquatable<SyncChunkData>, INetworkSerializable
{
	public Vector2Int SyncChunkPosition;
	public List<GenericGameObjectSyncData> SyncGroundTileDataList;
	public List<GenericGameObjectSyncData> SyncWallTileDataList;
	public List<GenericGameObjectSyncData> SyncObjectAssetDataList;
	public List<DoorObjectSyncData> SyncDoorObjectDataList;

	public bool Equals(SyncChunkData other)
	{
		return SyncChunkPosition == other.SyncChunkPosition &&
			   SyncGroundTileDataList.Equals(other.SyncGroundTileDataList) &&
			   SyncWallTileDataList.Equals(other.SyncWallTileDataList) &&
			   SyncObjectAssetDataList.Equals(other.SyncObjectAssetDataList) &&
			   SyncDoorObjectDataList.Equals(other.SyncDoorObjectDataList);
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref SyncChunkPosition);
		
		SerializeAgnosticDataList(serializer, ref SyncGroundTileDataList);
		SerializeAgnosticDataList(serializer, ref SyncWallTileDataList);
		SerializeAgnosticDataList(serializer, ref SyncObjectAssetDataList);
		SerializeDoorDataList(serializer, ref SyncDoorObjectDataList);
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

// For assets and Tiles. NTFS: Might need to make them separate later for now this works
public struct GenericGameObjectSyncData : IEquatable<GenericGameObjectSyncData>, INetworkSerializable
{
	public Vector2Int Position;
	public byte ID;

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

// For assets and Tiles. NTFS: Might need to make them separate later for now this works
public struct DoorObjectSyncData : IEquatable<DoorObjectSyncData>, INetworkSerializable
{
	public Vector2Int Position;
	public byte ID;
	public bool IsOpen;
	
	public bool Equals(DoorObjectSyncData other)
	{
		return Position.Equals(other.Position) && IsOpen == other.IsOpen && ID == other.ID;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref Position);
		serializer.SerializeValue(ref ID);
		serializer.SerializeValue(ref IsOpen);
	}
}
