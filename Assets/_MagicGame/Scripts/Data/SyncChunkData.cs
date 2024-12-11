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
    public List<GenericGameObjectSyncData> SyncWorldAssetDataList;

    public bool Equals(SyncChunkData other)
    {
        return SyncChunkPosition == other.SyncChunkPosition &&
               SyncGroundTileDataList.Equals(other.SyncGroundTileDataList) &&
               SyncWallTileDataList.Equals(other.SyncWallTileDataList) &&
               SyncWorldAssetDataList.Equals(other.SyncWorldAssetDataList);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SyncChunkPosition);
		
        SerializeAgnosticDataList(serializer, ref SyncGroundTileDataList);
        SerializeAgnosticDataList(serializer, ref SyncWallTileDataList);
        SerializeAgnosticDataList(serializer, ref SyncWorldAssetDataList);
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
    public byte ID; // NTFS: If tile amount ever go above 255, change this to ushort

    // Implementing Equals to compare two SyncTileData structs
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
