using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace ProjectTinker
{
    public class ChunkManager : NetworkBehaviour
    {
        public static ChunkManager Instance { get; private set; }

        public static bool IS_GENERATING_BIOME;
        public static int BIOME_SIDE_LENGTH = 192;
        public static int CHUNK_SIZE = 24;

        public class OnActiveChunksUpdatedEventArgs : EventArgs
        {
            public Vector2Int MinLoadedTilePos;
            public Vector2Int MaxLoadedTilePos;
        }
        public event EventHandler<ChunkEventArgs> OnLoadChunk;
        public event EventHandler<ChunkEventArgs> OnUnloadChunk;
        public class ChunkEventArgs : EventArgs
        {
            public ChunkGameData Chunk;
        }

        [field: SerializeField] public float TimeBetweenChunkLoads { get; private set; } = 0.0185f;


        private Dictionary<Vector2Int, ChunkGameData> _forestChunks = new(); // Data structure to hold chunk data
        private Dictionary<Vector2Int, ChunkGameData> _caveChunks = new(); // Data structure to hold chunk data
        private NetworkChunkManager _chunkNetworkManager;
        private List<ChunkGameData> _chunksToLoad = new();

        private void Awake()
        {
            Instance = this;

            _chunkNetworkManager = GetComponent<NetworkChunkManager>();
        }

        private void Start()
        {
            GameWorld.Instance.OnBiomeDataLoaded += StaggerChunkRequests;
        }

        public override void OnDestroy()
        {
            GameWorld.Instance.OnBiomeDataLoaded -= StaggerChunkRequests;
        }

        private List<Vector2Int> GetChunkPositions()
        {
            Vector2Int playerChunkPos = GetChunkCoordFromPosition(Player.Instance.transform.position);
            List<Vector2Int> chunkPositions = new();
            int numChunks = BIOME_SIDE_LENGTH / CHUNK_SIZE;

            for (int y = 0; y < numChunks; y++)
            {
                for (int x = 0; x < numChunks; x++)
                {
                    chunkPositions.Add(new Vector2Int(x, y));
                }
            }

            chunkPositions = chunkPositions.OrderBy(pos => Vector2Int.Distance(pos, playerChunkPos)).ToList();
            return chunkPositions;
        }

        private void StaggerChunkRequests(object sender, EventArgs e)
        {
            StartCoroutine(StaggerChunkRequests());
        }

        private IEnumerator StaggerChunkRequests()
        {
            _chunksToLoad = new List<ChunkGameData>((BIOME_SIDE_LENGTH / CHUNK_SIZE) * (BIOME_SIDE_LENGTH / CHUNK_SIZE));

            foreach (Vector2Int chunkPos in GetChunkPositions())
            {
                _chunkNetworkManager.RequestChunkDataServerRpc(Player.Instance.OwnerClientId, Player.Instance.CurrentBiome.Value, chunkPos);
                yield return new WaitForSeconds(TimeBetweenChunkLoads);
            }
        }

        public void LoadChunk(ChunkGameData chunkGameDataToLoad)
        {
            _chunksToLoad.Add(chunkGameDataToLoad);

            OnLoadChunk?.Invoke(this, new ChunkEventArgs
            {
                Chunk = chunkGameDataToLoad
            });

            if (_chunksToLoad.Count == (BIOME_SIDE_LENGTH / CHUNK_SIZE) * (BIOME_SIDE_LENGTH / CHUNK_SIZE))
            {
                Lightmap.Instance.UpdateLightMap();
                GameWorld.Instance.ExecuteOnBiomeChunksDoneLoading();
                Debug.Log($"ChunkManager: OnBiomeDataLoaded for {Player.Instance.CurrentBiome.Value}");
            }
        }

        public void UnloadAllPlayerChunks()
        {
            foreach (var item in GetChunksFromBiome(Player.Instance.CurrentBiome.Value))
            {
                OnUnloadChunk?.Invoke(this, new ChunkEventArgs
                {
                    Chunk = item.Value
                });
            }
        }

        public ChunkGameData GetChunkFromChunkPosition(BiomeType biome, Vector2Int chunkPosition)
        {
            switch (biome)
            {
                case BiomeType.Forest:

                    if (!_forestChunks.ContainsKey(chunkPosition) || _forestChunks[chunkPosition] == null)
                    {
                        Debug.LogError($"This should not be playing chunks should exist on requested");
                        return null;
                    }

                    return _forestChunks[chunkPosition];
                case BiomeType.Cave:

                    if (!_caveChunks.ContainsKey(chunkPosition) || _caveChunks[chunkPosition] == null)
                    {
                        Debug.LogError($"This should not be playing chunks should exist on requested");
                        return null;
                    }

                    return _caveChunks[chunkPosition];
            }

            Debug.LogError("No Environment found for _activeEnvironment variable");
            return null;
        }

        public bool SetDoorState(Vector2Int doorPos, BiomeType biome, bool isOpen)
        {
            if (!IsServer) return false;

            ChunkGameData chunk = GetChunkFromAnyWorldPos(doorPos, biome);

            foreach (ResourceObjectGameData worldObject in chunk.GetWorldObjects())
            {
                if (worldObject.Position == doorPos)
                {
                    // Found door
                    var doorObject = worldObject as DoorObjectGameData;
                    doorObject.SetDoorState(isOpen);

                    return doorObject.IsOpen;
                }
            }

            return false;
        }

        public void DeserializeObjectDataToChunk(ResourceObjectFileData worldObjectFileData, BiomeType biome, ResourceObject worldObject, CardinalDirection orientation)
        {
            if (!IsServer) return;

            ChunkGameData chunk = GetChunkFromAnyWorldPos(worldObjectFileData.Pos, biome);

            chunk.DeserializeObjectData(worldObjectFileData, worldObject, orientation);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void AddResourceDataToChunkServerRpc(Vector2Int position, ushort resourceId, BiomeType biomeToPlaceIn, CardinalDirection orientation)
        {
            ChunkGameData chunk = GetChunkFromAnyWorldPos(position, biomeToPlaceIn);

            ResourceObject worldObject = GameDataRegistry.Instance.GetResourceDataFromResourceId(resourceId).ResourcePrefab;
            chunk.AddObjectData(position, worldObject, orientation);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void RemoveRscDataFromChunkServerRpc(Vector2Int position, BiomeType biomeToRemoveFrom)
        {
            GetChunkFromAnyWorldPos(position, biomeToRemoveFrom).RemoveResourceData(position);
            TryToRemoveResourceClientRpc(position, biomeToRemoveFrom);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void TryToRemoveResourceClientRpc(Vector2Int position, BiomeType biomeToRemoveResourceData)
        {
            if (Player.Instance.CurrentBiome.Value != biomeToRemoveResourceData) return;

            if (ResourceManager.Instance.TryToFindResourceObject(position, out ResourceObject rsc))
            {
                rsc.ResourceFeedbacks.PlayClientDestructionSequence();
            }
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void PlaceTileServerRpc(Vector2Int position, ushort tileID, BiomeType biomeToAddTileData, TileType tileType)
        {
            ChunkGameData chunk = GetChunkFromAnyWorldPos(position, biomeToAddTileData);
            chunk.AddTileData(position, GameDataRegistry.Instance.GetTileDataFromTileId(tileID));

            if (tileType == TileType.Wall)
            {
                Pathfinding.Instance.AddPfWallTileServerRpc(position, biomeToAddTileData);
            }

            TileManager.Instance.HandleTileVisualClientRpc((Vector3Int)position, tileID, tileType, biomeToAddTileData, false);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        public void RemoveTileServerRpc(TileType tileType, Vector2Int position, BiomeType biome, bool playDestroyFeedbacks)
        {
            GetChunkFromAnyWorldPos(position, biome).RemoveTileData(position, tileType);

            if (tileType == TileType.Wall)
            {
                Pathfinding.Instance.RemovePathfindingfWallTileServerRpc(position, biome);
            }

            // NTFS: ushort.MinValue can be any number as long as render air is true here to render air
            TileManager.Instance.HandleTileVisualClientRpc((Vector3Int)position, GameDataRegistry.INVALID_ID, tileType, biome, playDestroyFeedbacks);
        }

        public ChunkGameData GetChunkFromAnyWorldPos(Vector2Int anyWorldPos, BiomeType biomeToGetChunkFrom)
        {
            Vector2Int chunkCoord = GetChunkCoordFromPosition(anyWorldPos);

            // Bounds check
            if (!IsWorldPosInBounds(anyWorldPos))
            {
                Debug.LogWarning($"GetChunkFromAnyWorldPos: Position {anyWorldPos} maps to invalid chunkCoord {chunkCoord}");
                return null;
            }

            if (!GetChunksFromBiome(biomeToGetChunkFrom).TryGetValue(chunkCoord, out ChunkGameData chunk))
            {
                Debug.LogWarning($"ChunkCoord {chunkCoord} was in bounds but no chunk was found. Possible desync?");
            }
            return chunk;
        }

        public bool IsWorldPosInBounds(Vector2Int worldPos)
        {
            return worldPos.x >= 0 && worldPos.y >= 0 &&
                   worldPos.x < BIOME_SIDE_LENGTH && worldPos.y < BIOME_SIDE_LENGTH;
        }

        private Vector2Int GetChunkCoordFromPosition(Vector2 position)
        {
            int chunkX = Mathf.FloorToInt(position.x / CHUNK_SIZE);
            int chunkY = Mathf.FloorToInt(position.y / CHUNK_SIZE);
            return new Vector2Int(chunkX, chunkY);
        }

        public Dictionary<Vector2Int, ChunkGameData> GetChunksFromBiome(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    return _forestChunks;
                case BiomeType.Cave:
                    return _caveChunks;
            }

            Debug.LogError($"Biome {biome} should exist but doesn't, add environment chunks to ChunkManager");
            return null;
        }

        public void LoadChunksForBiome(BiomeType biomeToSetChunksFor, Dictionary<Vector2Int, ChunkGameData> newChunks)
        {
            switch (biomeToSetChunksFor)
            {
                case BiomeType.Forest:
                    _forestChunks = newChunks;
                    return;
                case BiomeType.Cave:
                    _caveChunks = newChunks;
                    return;
            }

            Debug.LogError("No Biome found for _activeEnvironment variable");
        }
    }
}
