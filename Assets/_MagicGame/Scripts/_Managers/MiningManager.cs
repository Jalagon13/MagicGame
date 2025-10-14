using System;
using System.Collections;
using FMODUnity;
using UnityEngine;
using EventHandler = System.EventHandler;

namespace ProjectWizard
{
    public class MiningManager : MonoBehaviour
    {
        public static MiningManager Instance { get; private set; }

        public event EventHandler OnDetectMineablesStarted;
        public event EventHandler OnDetectMineablesStopped;
        public event EventHandler OnMiningStopped;
        public event EventHandler<MiningStartedEventArgs> OnMiningStarted;
        public class MiningStartedEventArgs : EventArgs
        {
            public Vector3Int BreakTargetPosition;
            public BiomeType Biome;
            public TileDataSO TileData;
            public ResourceDataSO ResourceData;
            public float TotalMiningTime;

            public MiningStartedEventArgs(Vector3Int breakTargetPosition, BiomeType biome, TileDataSO tileData, ResourceDataSO resourceData, float totalMiningTime)
            {
                BreakTargetPosition = breakTargetPosition;
                Biome = biome;
                TileData = tileData;
                TotalMiningTime = totalMiningTime;
                ResourceData = resourceData;
            }
        }
        
        [SerializeField]
        private float _timeBetweenMiningSounds = 0.225f/* , _delayBetweenPlacingAndMining = 0.2f */, _breakCooldownDuration = 0.075f;

        private ToolItemSO _currentToolItemSO;
        public ToolItemSO CurrentToolItemSO => _currentToolItemSO;

        private const float _timesPerSecond = 30f;
        private readonly float _intervalSeconds = 1f / _timesPerSecond;

        private MiningState _currentState = MiningState.Idle;
        private enum MiningState
        {
            Idle,
            Detecting
        }

        private Vector2Int? _lastCheckedTilePosition = null;
        private Coroutine _currentMiningCoroutine;
        private Timer _breakCooldownTimer;
        
        private MiningMode _currentMiningMode = MiningMode.Manual;
        private enum MiningMode
        {
            Manual,
            Smart
        }

        private void Awake()
        {
            Instance = this;
            _breakCooldownTimer = new(_breakCooldownDuration);
        }

        private void Start()
        {
            HotbarManager.Instance.OnFocusSlotUpdated += DetectMiningSpell;
            GameInput.Instance.OnPrimaryAction += DetectMiningInput;
            GameInput.Instance.OnToggleMiningMode += ToggleMiningMode;
        }

        private void OnDestroy()
        {
            HotbarManager.Instance.OnFocusSlotUpdated -= DetectMiningSpell;
            GameInput.Instance.OnPrimaryAction -= DetectMiningInput;
            GameInput.Instance.OnToggleMiningMode -= ToggleMiningMode;
        }

        private void Update()
        {
            _breakCooldownTimer.Tick(Time.deltaTime);
        }

        private void ToggleMiningMode(object sender, EventArgs e)
        {
            _currentMiningMode = _currentMiningMode == MiningMode.Manual ? MiningMode.Smart : MiningMode.Manual;
            Debug.Log($"Toggling mining mode to {_currentMiningMode}");
            
        }

        private void DetectMiningInput(object sender, GameInput.OnPrimaryOrSecondaryActionEventArgs e)
        {
            if (_currentToolItemSO == null) return;

            SetState(e.IsHeldDown ? MiningState.Detecting : MiningState.Idle);
        }

        private void DetectMiningSpell(object sender, HotbarManager.OnFocusItemSetEventArgs e)
        {
            ItemDataSO itemData = GameDataRegistry.Instance.GetItemDataFromItemId(e.SelectedItemId);
            if (itemData == null || (_currentToolItemSO != null && itemData.StringID == _currentToolItemSO.StringID)) return;

            if (itemData is ToolItemSO toolItemSO)
            {
                _currentToolItemSO = toolItemSO;
                if (GameInput.Instance.GetPrimaryHeldDown())
                {
                    SetState(MiningState.Detecting);
                }
            }
            else
            {
                _currentToolItemSO = null;
                SetState(MiningState.Idle);
            }
        }

        private void SetState(MiningState newState)
        {
            if (_currentState == newState) return;

            // Stop detection loop
            CancelInvoke(nameof(CheckForMineables));

            _currentState = newState;

            if (_currentState == MiningState.Detecting)
            {
                OnDetectMineablesStarted?.Invoke(this, EventArgs.Empty);
                InvokeRepeating(nameof(CheckForMineables), 0f, _intervalSeconds);
            }
            else
            {
                OnDetectMineablesStopped?.Invoke(this, EventArgs.Empty);

                if (_currentMiningCoroutine != null)
                {
                    StopCoroutine(_currentMiningCoroutine);
                    _currentMiningCoroutine = null;
                    OnMiningStopped?.Invoke(this, EventArgs.Empty);
                }

                _lastCheckedTilePosition = null;
            }
        }

        // NTFS: refine the smart mining mode later it kind of sucks right now but it works
        private void CheckForMineables()
        {
            if (_currentToolItemSO == null || _breakCooldownTimer.RemainingSeconds > 0)
                return;

            Vector2Int currentPos;

            if (_currentMiningMode == MiningMode.Smart)
            {
                Vector2Int playerTilePos = Vector2Int.FloorToInt(Player.Instance.transform.position);
                Vector2Int mouseTilePos = (Vector2Int)ActionManager.MouseTilePosition;

                Vector2 targetPoint;

                if (_currentToolItemSO.PlayerWithinMiningRangeOfMouse())
                {
                    targetPoint = mouseTilePos;
                }
                else
                {
                    Vector2 playerWorldPos = Player.Instance.transform.position;
                    Vector2 mouseWorldPos = ActionManager.MouseWorldPosition;
                    Vector2 dir = (mouseWorldPos - playerWorldPos).normalized;
                    float range = _currentToolItemSO.MiningRange;
                    targetPoint = playerWorldPos + dir * range;
                }

                Vector2Int targetTilePos = Vector2Int.RoundToInt(targetPoint);

                // Thick Bresenham-style line to create connected 1-tile-wide path including diagonals
                Vector2Int current = playerTilePos;
                Vector2Int delta = new Vector2Int(Mathf.Abs(targetTilePos.x - playerTilePos.x), Mathf.Abs(targetTilePos.y - playerTilePos.y));
                int stepX = playerTilePos.x < targetTilePos.x ? 1 : -1;
                int stepY = playerTilePos.y < targetTilePos.y ? 1 : -1;

                int err = delta.x - delta.y;
                int prevErr = err;

                Vector2Int foundWallTile = targetTilePos;
                bool found = false;

                while (true)
                {
                    // Check current tile for wall
                    if (TileManager.Instance.WallTm.HasTile((Vector3Int)current))
                    {
                        foundWallTile = current;
                        found = true;
                        break;
                    }

                    if (current == targetTilePos)
                        break;

                    int e2 = 2 * err;
                    bool movedDiagonally = false;

                    if (e2 > -delta.y && e2 < delta.x)
                    {
                        // Moving diagonally
                        movedDiagonally = true;
                    }

                    if (e2 > -delta.y)
                    {
                        err -= delta.y;
                        current.x += stepX;
                    }
                    if (e2 < delta.x)
                    {
                        err += delta.x;
                        current.y += stepY;
                    }

                    // If moved diagonally, also check the neighbor tile to maintain connectivity
                    if (movedDiagonally)
                    {
                        // Determine which neighbor to check for connectivity
                        // Choose the tile along the axis with smaller delta or previous step axis
                        Vector2Int neighbor;

                        int deltaX = delta.x;
                        int deltaY = delta.y;

                        if (deltaX < deltaY)
                        {
                            // Move horizontally neighbor
                            neighbor = new Vector2Int(current.x - stepX, current.y);
                        }
                        else if (deltaY < deltaX)
                        {
                            // Move vertically neighbor
                            neighbor = new Vector2Int(current.x, current.y - stepY);
                        }
                        else
                        {
                            // If equal, use previous error to decide
                            if (prevErr > err)
                            {
                                neighbor = new Vector2Int(current.x - stepX, current.y);
                            }
                            else
                            {
                                neighbor = new Vector2Int(current.x, current.y - stepY);
                            }
                        }

                        prevErr = err;

                        if (TileManager.Instance.WallTm.HasTile((Vector3Int)neighbor))
                        {
                            foundWallTile = neighbor;
                            found = true;
                            break;
                        }
                    }
                    else
                    {
                        prevErr = err;
                    }
                }

                currentPos = found ? foundWallTile : targetTilePos;
            }
            else
            {
                if (!_currentToolItemSO.PlayerWithinMiningRangeOfMouse())
                    return;

                currentPos = (Vector2Int)ActionManager.MouseTilePosition;
            }
            
            Debug.Log($"Checking for mineables at {currentPos}");

            // Start a new mining sequence if we're on a new tile or if the current mining coroutine has ended
            if (_lastCheckedTilePosition == null || currentPos != _lastCheckedTilePosition.Value || _currentMiningCoroutine == null)
            {
                // Stop the previous mining coroutine if it exists
                if (_currentMiningCoroutine != null)
                {
                    StopCoroutine(_currentMiningCoroutine);
                    OnMiningStopped?.Invoke(this, EventArgs.Empty);
                    _currentMiningCoroutine = null;
                    _breakCooldownTimer.Reset();
                }
                else
                {
                    _lastCheckedTilePosition = currentPos;

                    if (!TryMineResource(currentPos))
                    {
                        TryMineWall(currentPos);
                    }
                }
            }
        }

        private bool TryMineResource(Vector2Int pos)
        {
            if (ResourceManager.Instance.TryToFindResourceObject(pos, out ResourceObject ro) && ro.Data.CanBeDestroyed)
            {
                _currentMiningCoroutine = StartCoroutine(MiningSequence(
                    ro.Data.Hardness, null, ro.Data,
                    () =>
                    {
                        // First, play the mining sound
                        SoundManager.Instance.PlayOneShot(ro.Data.MiningSound, (Vector3Int)_lastCheckedTilePosition.Value);
                        ro.ResourceFeedbacks.PlayHitFeedback();
                    },
                    () =>
                    {
                        // Destroy the resource
                        ResourceManager.Instance.DestroyResourceServerRpc(
                            Player.Instance.CurrentBiome.Value,
                            _lastCheckedTilePosition.Value,
                            GameDataRegistry.Instance.GetResourceIdFromResourceData(ro.Data)
                        );
                    }
                ));
                return true;
            }
            return false;
        }

        private bool TryMineWall(Vector2Int pos)
        {
            Vector3Int wallPos = (Vector3Int)(Pointer.IsOverTopTile() ? pos + Vector2Int.down : pos);

            if (TileManager.Instance.WallTm.HasTile(wallPos))
            {
                TileDataSO tileData = GameDataRegistry.Instance.GetTileDataFromTileBase(TileManager.Instance.WallTm.GetTile(wallPos));

                _currentMiningCoroutine = StartCoroutine(MiningSequence(
                    tileData.Hardness, tileData, null,
                    () =>
                    {
                        // First, play the mining sound
                        SoundManager.Instance.PlayOneShot(tileData.MiningSound, wallPos);
                    },
                    () =>
                    {
                        // Then, destroy the wall tile
                        TileManager.Instance.DestroyTile((Vector2Int)wallPos, GameDataRegistry.Instance.GetTileIdFromTileData(tileData), Player.Instance.CurrentBiome.Value, true);
                    }
                ));
                return true;
            }
            return false;
        }

        private IEnumerator MiningSequence(float hardness, TileDataSO tileData, ResourceDataSO resourceData, Action playMiningSound, Action handleDestruction)
        {
            float totalTicks = hardness * 30f / Mathf.Max(_currentToolItemSO.MiningPower, 0.1f);
            float totalMiningTime = totalTicks * 0.05f;

            OnMiningStarted?.Invoke(this, new MiningStartedEventArgs(_lastCheckedTilePosition.HasValue ? (Vector3Int)_lastCheckedTilePosition.Value : Vector3Int.zero, Player.Instance.CurrentBiome.Value, tileData, resourceData, totalMiningTime));

            float elapsedTime = 0f;
            float nextSoundTime = 0f;

            // Play the first mining sound immediately
            playMiningSound();
            nextSoundTime += _timeBetweenMiningSounds;

            while (elapsedTime < totalMiningTime)
            {
                // Check if player is still within mining range
                if (_currentToolItemSO == null || (_currentMiningMode != MiningMode.Smart && !_currentToolItemSO.PlayerWithinMiningRangeOfMouse()))
                {
                    OnMiningStopped?.Invoke(this, EventArgs.Empty);
                    _currentMiningCoroutine = null;
                    yield break;
                }

                elapsedTime += Time.deltaTime;

                // Play mining sounds at intervals
                if (elapsedTime >= nextSoundTime)
                {
                    playMiningSound();
                    nextSoundTime += _timeBetweenMiningSounds;
                }

                yield return null;
            }

            // Mining finished
            handleDestruction();
            OnMiningStopped?.Invoke(this, EventArgs.Empty);
            _currentMiningCoroutine = null;
        }
    }
}