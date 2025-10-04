using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

namespace ProjectWizard
{
    [Flags]
    public enum BiomeType // NTFS: When adding new IDs remember to put the value to the next power of 2 for the [Flags] to work properly
    {
        None = 0,
        Forest = 1,
        Cave = 2,
    }

    public class GameWorld : NetworkBehaviour
    {
        public static GameWorld Instance { get; private set; }

        public event EventHandler OnBiomeTransitionStart;
        public event EventHandler OnBiomeTransitionEnd;
        public event EventHandler OnBiomeDataLoaded;
        public event EventHandler<OnTickEventArgs> OnTick;
        public class OnTickEventArgs : EventArgs
        {
            public float CurrentDayRatio;
            public float CurrentTime;
            public float DayDuration;
        }

        [Header("Time Settings")]
        [SerializeField]
        private bool _isTicking = true;
        [SerializeField]
        private float _dayDurationInSeconds, _startingTime = 0.0f;

        [Header("World Settings")]
        [SerializeField]
        private string _customSeed = 123.ToString();
        [SerializeField]
        private bool _randomSeed = false;

        [Header("Generators")]
        [SerializeField]
        private ForestGenerator _forestGenerator;
        [SerializeField]
        private CaveGenerator _caveGenerator;

        private float _currentTime;

        public bool IsNight { get; private set; }
        public bool IsLoadingBiome { get; private set; }
        public string Seed
        {
            get
            {
                return _randomSeed ? Time.time.ToString() : _customSeed;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected_SyncTime;
            }

            _currentTime = _startingTime;

            // We need to ensure that we don't have a day length at 0, otherwise we will get stuck into infinite loop in update.
            if (_dayDurationInSeconds <= 0.0f)
            {
                // (and a day with 0 length makes no sense)
                _dayDurationInSeconds = 1.0f;
            }

            yield return new WaitForEndOfFrame();

            Tick(); // Tick once to trigger initial light update
        }

        private void Update()
        {
            if (_isTicking)
                Tick();
        }

        private void Tick()
        {
            _currentTime += Time.deltaTime;

            while (_currentTime > _dayDurationInSeconds)
            {
                _currentTime -= _dayDurationInSeconds;

                // Resync time for all clients
                if (IsServer)
                {
                    foreach (var clientId in NetworkManager.ConnectedClientsIds)
                    {
                        SyncTimeForClientRpc(_currentTime, _dayDurationInSeconds, RpcTarget.Single(clientId, RpcTargetUse.Persistent));
                    }
                }
            }

            float currentDayRatio = _currentTime / _dayDurationInSeconds;

            IsNight = currentDayRatio >= 0.5f && currentDayRatio < 1f; // Set IsNight to true if the current ratio is between 0.5 (halfway) and 1 (end of the day)

            OnTick?.Invoke(this, new OnTickEventArgs
            {
                CurrentDayRatio = currentDayRatio,
                CurrentTime = _currentTime,
                DayDuration = _dayDurationInSeconds
            });
        }

        private void OnClientConnected_SyncTime(ulong clientId)
        {
            SyncTimeForClientRpc(_currentTime, _dayDurationInSeconds, RpcTarget.Single(clientId, RpcTargetUse.Persistent));
        }

        [Rpc(SendTo.SpecifiedInParams, RequireOwnership = false)]
        private void SyncTimeForClientRpc(float currentTime, float dayDurationInSeconds, RpcParams rpcParams)
        {
            _currentTime = currentTime;
            _dayDurationInSeconds = dayDurationInSeconds;
        }

        public void LoadBiome(BiomeType targetBiome, Vector2 position)
        {
            PlacePlayerAt(position);

            OnBiomeTransitionStart?.Invoke(this, EventArgs.Empty);

            IsLoadingBiome = true;

            if (Player.Instance.CurrentBiome.Value != BiomeType.None)
            {
                ChunkManager.Instance.UnloadAllPlayerChunks();
                // NTFS: Need to figure out a stagger unload biome object visuals so its not all getting deleted in one frame
                ResourceManager.Instance.ClearAllBiomeObjectVisuals();
            }

            LoadBiomeServerRpc(Player.Instance.CurrentBiome.Value, targetBiome);
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void LoadBiomeServerRpc(BiomeType fromBiome, BiomeType toBiome, RpcParams rpcParams = default)
        {
            AsyncLoadBiome(fromBiome, toBiome, rpcParams);
        }

        private async void AsyncLoadBiome(BiomeType fromBiome, BiomeType toBiome, RpcParams rpcParams = default)
        {
            Debug.Log($"Executing biome load from {fromBiome} to {toBiome}");

            // Save the last biome it came from and set the player's burrent biome to tobiome.
            if (!SaveSystem.Instance.IsSaving && SaveSystem.Instance.BiomeLoadedInMemory(fromBiome))
            {
                await SaveSystem.Instance.SaveBiome(fromBiome);
            }

            // If the targetBiome is already loaded into memory, 
            if (SaveSystem.Instance.BiomesInMemory.Contains(toBiome))
            {
                Debug.Log($"Biome already exists in memory: {toBiome}. loading chunks...");
                LoadChunksClientRpc(toBiome, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
            }
            else
            {
                // If not, deserializeanddispatch data
                if (SaveSystem.Instance.BiomeSaveFileExists(toBiome))
                {
                    await SaveSystem.Instance.DeserializeAndDispatchData(toBiome);
                }
                else
                {
                    GenerateBiome(toBiome);

                    await SaveSystem.Instance.SaveBiome(toBiome);
                }

                LoadChunksClientRpc(toBiome, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Persistent));
            }
        }

        private void GenerateBiome(BiomeType toBiome)
        {
            switch (toBiome)
            {
                case BiomeType.Forest:
                    _forestGenerator.GenerateForest();
                    break;
                case BiomeType.Cave:
                    _caveGenerator.GenerateCave();
                    break;
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void LoadChunksClientRpc(BiomeType toBiome, RpcParams rpcParams)
        {
            Player.Instance.CurrentBiome.Value = toBiome;

            // Invoke it first to prep the last chunk position to garentee a new set of chunks to generate, then set loadingbiome to true to resume the update method
            TileManager.Instance.UpperWallTm.ClearAllTopTiles();
            OnBiomeDataLoaded?.Invoke(this, EventArgs.Empty);
        }

        public void ExecuteOnBiomeChunksDoneLoading() // Yeah this is scuffed i know
        {
            IsLoadingBiome = false;
            OnBiomeTransitionEnd?.Invoke(this, EventArgs.Empty);
        }

        private void PlacePlayerAt(Vector2 portalPosition)
        {
            Player.Instance.transform.SetPositionAndRotation(new(portalPosition.x + 0.5f, portalPosition.y + 0.5f), Quaternion.identity);
        }


        public bool IsTicking()
        {
            return _isTicking;
        }

        public override void OnDestroy()
        {
            if (IsServer)
            {
                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected_SyncTime;
                }
            }

            base.OnDestroy();
        }
    }
}
