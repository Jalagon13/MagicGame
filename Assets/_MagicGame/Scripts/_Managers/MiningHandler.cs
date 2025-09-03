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
        public Vector3Int BreakTargetPosition;
        public float TotalMiningTime;
        public BiomeType Biome;
        public DestructableType DestructableType;

        public MiningStartedEventArgs(float totalMiningTime, Vector3Int breakTargetPosition, BiomeType biome, DestructableType destructableType)
        {
            TotalMiningTime = totalMiningTime;
            BreakTargetPosition = breakTargetPosition;
            Biome = biome;
            DestructableType = destructableType;
        }
    }
    
    [field: SerializeField] public float TimeBetweenMiningSounds { get; private set; } = 0.25f;
    [field: SerializeField] public float DelayBetweenPlacingAndMining { get; private set; } = 0.15f;
    [field: SerializeField] public float BreakCooldownDuration { get; private set; } = 0.15f;
    public bool IsMining { get; private set; }

    private Timer _miningTimer, _miningSoundTimer, _breakCooldownTimer;
    private DestructableType _destructableFound;
    private ResourceObject _resourceSelected;
    private TileDataSO _tileSelected;
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
        return Player.Instance != null &&
            //    !Player.LocalClientInstance.HealthState.IsDead &&
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

        if (ResourceManager.Instance.TryToFindResourceObject((Vector2Int)pos, out ResourceObject wo) && wo.Data.CanBeDestroyed)
        {
            _resourceSelected = wo;
            _destructableFound = DestructableType.WorldObject;
            _selectedResourceToolType = wo.Data.ToolTypeNeededForHarvest;
            _currentBreakTargetPosition = pos;
        }
        else if (TileManager.Instance.WallTm.HasTile(wallPos))
        {
            _tileSelected = TileManager.Instance.OreTm.HasTile(wallPos)
                ? GameDataRegistry.Instance.GetTileDataFromTileBase(TileManager.Instance.OreTm.GetTile(wallPos))
                : GameDataRegistry.Instance.GetTileDataFromTileBase(TileManager.Instance.WallTm.GetTile(wallPos));

            _destructableFound = DestructableType.Tile;
            _selectedResourceToolType = _tileSelected.ToolTypeNeededForHarvest;
            _currentBreakTargetPosition = wallPos;
        }
        else if (TileManager.Instance.FloorTm.HasTile(pos))
        {
            _tileSelected = GameDataRegistry.Instance.GetTileDataFromTileBase(TileManager.Instance.FloorTm.GetTile(pos));

            _destructableFound = DestructableType.Tile;
            _selectedResourceToolType = _tileSelected.ToolTypeNeededForHarvest;
            _currentBreakTargetPosition = pos;
        }
        else
        {
            
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
        IsMining = true;
        _originalBreakTargetPosition = _currentBreakTargetPosition;

        float hardness = _destructableFound switch
        {
            DestructableType.WorldObject => _resourceSelected.Data.Hardness,
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
            
            _miningTimer = new Timer(totalMiningTime);
            _miningTimer.OnTimerEnd -= DestroyResource;
            _miningTimer.OnTimerEnd += DestroyResource;
        }
    }

    private void OnMiningStateChanged()
    {
        if (IsMining)
        {
            OnMiningStarted?.Invoke(this, new MiningStartedEventArgs(_cachedTotalMiningTime, (Vector3Int)_currentBreakTargetPosition, Player.Instance.CurrentBiome.Value, _destructableFound));
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
                ResourceManager.Instance.DestroyResourceServerRpc(Player.Instance.CurrentBiome.Value, (Vector2Int)_currentBreakTargetPosition, GameDataRegistry.Instance.GetUShortIdFromResourceData(_resourceSelected.Data));
                break;
            case DestructableType.Tile:
                TileManager.Instance.DestroyTile((Vector2Int)_currentBreakTargetPosition, GameDataRegistry.Instance.GetUShortIdFromTileData(_tileSelected), Player.Instance.CurrentBiome.Value);
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
                SoundManager.Instance.PlayOneShot(_resourceSelected.Data.MiningSound, (Vector3)_currentBreakTargetPosition);
                _resourceSelected.ResourceFeedbacks.PlayHitFeedback();
                break;
            case DestructableType.Tile:
                SoundManager.Instance.PlayOneShot(_tileSelected.MiningSound, (Vector3)_currentBreakTargetPosition);
                break;
        }
    }
}
