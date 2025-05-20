using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

public enum DestructableType
{
    Tile,
    WorldObject,
    None
}

public class MiningHandler : MonoBehaviour
{
    public static MiningHandler Instance { get; private set; }
    
    public event System.EventHandler OnMiningStopped;
    public event EventHandler<MiningStartedEventArgs> OnMiningStarted;
    public class MiningStartedEventArgs : EventArgs
    {
        public Vector2Int BreakTargetPosition;
        public float TotalMiningTime;
        public BiomeType Biome;

        public MiningStartedEventArgs(float totalMiningTime, Vector2Int breakTargetPosition, BiomeType biome)
        {
            TotalMiningTime = totalMiningTime;
            BreakTargetPosition = breakTargetPosition;
            Biome = biome;
        }
    }
    
    [field: SerializeField] public float TimeBetweenMiningSounds { get; private set; } = 0.25f;
    [field: SerializeField] public float DelayBetweenPlacingAndMining { get; private set; } = 0.15f;
    [field: SerializeField] public float BreakCooldownDuration { get; private set; } = 0.15f;
    public bool IsMining { get; private set; }

    private Timer _miningTimer, _miningSoundTimer, _breakCooldownTimer;
    private DestructableType _destructableFound;
    private WorldObject _worldObjectSelected;
    private TileSO _tileSelected;
    private Vector3Int? _currentBreakTargetPosition = null;
    private Vector3Int? _originalBreakTargetPosition = null;
    private bool _placeDelayActive;
    private float _cachedTotalMiningTime;
    private ToolType _playerToolType, _selectedResourceToolType;

    private void Awake()
    {
        Instance = this;

        _miningTimer = new Timer(0f);
        _breakCooldownTimer = new Timer(0f);
        _miningSoundTimer = new Timer(TimeBetweenMiningSounds);
        _miningSoundTimer.OnTimerEnd += PlayMiningFeedbacks;
    }

    private void LateUpdate()
    {
        if (!CanMine()) return;
        if (DeployItemSO.PlacedThisFrameFlag)
        {
            if (!_placeDelayActive)
            {
                StartCoroutine(SmallPlacementDelay());
            }
            return;
        }

        _breakCooldownTimer.Tick(Time.deltaTime);

        if(InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedItem) && selectedItem.Item is ToolItemSO toolItemSO)
        {
            _playerToolType = toolItemSO.ToolType;
            
            DetectTarget(toolItemSO);
            HandleMiningLogic(toolItemSO);
        }
        else
        {
            _playerToolType = ToolType.None;
        }
    }

    private bool CanMine()
    {
        return Player.LocalClientInstance != null &&
               !Player.LocalClientInstance.HealthState.IsDead &&
               !Pointer.IsOverUI() &&
               GameInput.Instance.GetInputsEnabled() &&
               InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
    }

    private void DetectTarget(ToolItemSO toolItemSO)
    {
        _destructableFound = DestructableType.None;
        _selectedResourceToolType = ToolType.None;
        _currentBreakTargetPosition = null;

        if (!toolItemSO.PlayerWithinMiningRangeOfMouse()) return;

        Vector3Int pos = ActionManager.MouseTilePosition;
        Vector3Int wallPos = Pointer.IsOverTopTile() ? pos + Vector3Int.down : pos;

        if (ObjectManager.Instance.TryToFindWorldObject((Vector2Int)pos, out WorldObject wo) && wo.CanBeDestroyed)
        {
            _worldObjectSelected = wo;
            _destructableFound = DestructableType.WorldObject;
            _selectedResourceToolType = wo.ToolTypeNeededForHarvest;
            _currentBreakTargetPosition = pos;
        }
        else if (TileRenderManager.Instance.WallTm.HasTile(wallPos))
        {
            _tileSelected = TileRenderManager.Instance.OreTm.HasTile(wallPos)
                ? GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.OreTm.GetTile(wallPos))
                : GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.WallTm.GetTile(wallPos));

            _destructableFound = DestructableType.Tile;
            _selectedResourceToolType = _tileSelected.ToolTypeNeededForHarvest;
            _currentBreakTargetPosition = wallPos;
        }
        else if (TileRenderManager.Instance.FloorTm.HasTile(pos))
        {
            _tileSelected = GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.FloorTm.GetTile(pos));
            
            _destructableFound = DestructableType.Tile;
            _selectedResourceToolType = _tileSelected.ToolTypeNeededForHarvest;
            _currentBreakTargetPosition = pos;
        }
    }

    private void HandleMiningLogic(ToolItemSO toolItemSO)
    {
        bool wasMining = IsMining;

        if (_destructableFound == DestructableType.None || _breakCooldownTimer.RemainingSeconds > 0)
        {
            IsMining = false;
        }
        else if (GameInput.Instance.GetPrimaryHeldDown() && _selectedResourceToolType == _playerToolType)
        {
            if (!IsMining)
            {
                BeginMining(toolItemSO);
            }

            if (IsMining)
            {
                if (_currentBreakTargetPosition != _originalBreakTargetPosition)
                {
                    IsMining = false;
                    _originalBreakTargetPosition = null;
                    _breakCooldownTimer.RemainingSeconds = BreakCooldownDuration;
                    OnMiningStateChanged();
                    return;
                }

                _miningTimer.Tick(Time.deltaTime);
                _miningSoundTimer.Tick(Time.deltaTime);
            }
        }
        else
        {
            IsMining = false;
            _originalBreakTargetPosition = null;
        }

        if (wasMining != IsMining)
        {
            OnMiningStateChanged();
        }
    }

    private void BeginMining(ToolItemSO toolItemSO)
    {
        Debug.Log($"Begin Mining {_destructableFound} at {_currentBreakTargetPosition}");
        IsMining = true;
        _originalBreakTargetPosition = _currentBreakTargetPosition;

        float hardness = _destructableFound switch
        {
            DestructableType.WorldObject => _worldObjectSelected.Hardness,
            DestructableType.Tile => _tileSelected.Hardness,
            _ => 1f
        };

        float totalTicks = hardness * 30f / Mathf.Max(toolItemSO.MiningPower, 0.1f);
        float totalMiningTime = totalTicks * 0.05f;
        _cachedTotalMiningTime = totalMiningTime;

        if (totalMiningTime == 0)
        {
            DestroyResource(null, null);
        }
        else
        {
            PlayMiningFeedbacks(null, null);
            Debug.Log($"Mining {_destructableFound} at {_currentBreakTargetPosition} for {totalMiningTime} seconds");
            _miningTimer = new Timer(totalMiningTime);
            _miningTimer.OnTimerEnd -= DestroyResource;
            _miningTimer.OnTimerEnd += DestroyResource;
        }
    }

    private void OnMiningStateChanged()
    {
        if (IsMining)
        {
            OnMiningStarted?.Invoke(this, new MiningStartedEventArgs(_cachedTotalMiningTime, (Vector2Int)_currentBreakTargetPosition, Player.LocalClientInstance.CurrentPlayerBiome.Value));
        }
        else
        {
            OnMiningStopped?.Invoke(this, EventArgs.Empty);
        }
    }

    private IEnumerator SmallPlacementDelay()
    {
        _placeDelayActive = true;
        
        yield return new WaitForSeconds(DelayBetweenPlacingAndMining);

        _placeDelayActive = false;
        DeployItemSO.PlacedThisFrameFlag = false;
    }
    
    private void DestroyResource(object sender, EventArgs e)
    {
        _miningTimer.OnTimerEnd -= DestroyResource;
        
        switch (_destructableFound)
        {
            case DestructableType.WorldObject:
                ObjectManager.Instance.DestroyObjectServerRpc(Player.LocalClientInstance.CurrentPlayerBiome.Value, (Vector2Int)_currentBreakTargetPosition, GameManager.Instance.GetIDFromWorldObject(_worldObjectSelected));
                Debug.Log($"Destroyed {_worldObjectSelected} at {_currentBreakTargetPosition}");
                break;
            case DestructableType.Tile:
                TileRenderManager.Instance.DestroyTileServerRpc((Vector2Int)_currentBreakTargetPosition, GameManager.Instance.GetTileIdFromTileSO(_tileSelected), Player.LocalClientInstance.CurrentPlayerBiome.Value);
                Debug.Log($"Destroyed {_tileSelected} at {_currentBreakTargetPosition}");
                break;
        }
        
        _breakCooldownTimer.RemainingSeconds = BreakCooldownDuration;
    }

    private void PlayMiningFeedbacks(object sender, EventArgs e)
    {
        _miningSoundTimer.RemainingSeconds = TimeBetweenMiningSounds;

        switch (_destructableFound)
        {
            case DestructableType.WorldObject:
                SoundManager.Instance.PlayOneShot(_worldObjectSelected.MiningSound, (Vector3)_currentBreakTargetPosition);
                _worldObjectSelected.PlayHitFeedback();
                break;
            case DestructableType.Tile:
                SoundManager.Instance.PlayOneShot(_tileSelected.MiningSound, (Vector3)_currentBreakTargetPosition);
                break;
        }
    }
}
