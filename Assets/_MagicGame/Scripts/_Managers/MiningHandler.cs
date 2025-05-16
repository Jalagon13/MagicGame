using System;
using System.Collections;
using FMODUnity;
using UnityEngine;

public enum DestructableType
{
    Tile,
    WorldObject,
    Npc,
    None
}

public class MiningHandler : MonoBehaviour
{
    public static MiningHandler Instance { get; private set; }
    public static bool FocusingOnWall = true;
    
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
    [field: SerializeField] public EventReference FocusWallSound { get; private set; }
    [field: SerializeField] public EventReference FocusFloorSound { get; private set; }
    public bool IsMining { get; private set; }

    private Timer _miningTimer, _miningSoundTimer, _breakCooldownTimer;
    private DestructableType _destructableFound;
    private WorldObject _worldObjectSelected;
    private TileSO _tileSelected;
    private Vector3Int? _currentBreakTargetPosition = null;
    private Vector3Int? _originalBreakTargetPosition = null;
    private bool _placeDelayActive;
    private Npc _selectedNPC;
    private float _cachedTotalMiningTime;

    private void Awake()
    {
        Instance = this;

        _miningTimer = new Timer(0f);
        _breakCooldownTimer = new Timer(0f);
        _miningSoundTimer = new Timer(TimeBetweenMiningSounds);
        _miningSoundTimer.OnTimerEnd += PlayMiningSound;
    }

    private void Start()
    {
        GameInput.Instance.OnMiningFocusToggled += ToggleTileFocus;
    }

    private void ToggleTileFocus(object sender, EventArgs e)
    {
        FocusingOnWall = !FocusingOnWall;
        
        if(FocusingOnWall)
        {
            SoundManager.Instance.PlayOneShot(FocusWallSound, Player.LocalClientInstance.transform.position);
        }
        else
        {
            SoundManager.Instance.PlayOneShot(FocusFloorSound, Player.LocalClientInstance.transform.position);
        }
    }

    private void LateUpdate()
    {
        if (!CanMine(out InventoryItem selectedInventoryItem)) return;
        if (DeployItemSO.PlacedThisFrameFlag)
        {
            if (!_placeDelayActive)
            {
                StartCoroutine(SmallPlacementDelay());
            }
            return;
        }

        _breakCooldownTimer.Tick(Time.deltaTime);

        if (IsHoldingMiningWand(selectedInventoryItem, out MiningSpellItemSO miningSpellItemSO, out int miningSpellSlotIndex))
        {
            DetectTarget(miningSpellItemSO);
            HandleMiningLogic(miningSpellItemSO, miningSpellSlotIndex);
        }
    }

    private bool CanMine(out InventoryItem selectedInventoryItem)
    {
        selectedInventoryItem = default;
        return Player.LocalClientInstance != null &&
               !Player.LocalClientInstance.HealthState.IsDead &&
               !Pointer.IsOverUI() &&
               GameInput.Instance.GetInputsEnabled() &&
               InventoryManager.Instance.SelectedItemExists(out selectedInventoryItem);
    }

    private bool IsHoldingMiningWand(InventoryItem item, out MiningSpellItemSO miningSpell, out int miningSpellSlotIndex)
    {
        miningSpell = null;
        miningSpellSlotIndex = -1;

        if (item.Item is WandItemSO && SpellManager.Instance.HasMiningSpell(out MiningSpellItemSO spell, out int slotIndex))
        {
            miningSpell = spell;
            miningSpellSlotIndex = slotIndex;
            return true;
        }

        return false;
    }

    private void DetectTarget(MiningSpellItemSO miningSpellItemSO)
    {
        _destructableFound = DestructableType.None;
        _currentBreakTargetPosition = null;

        if (!miningSpellItemSO.PlayerWithinMiningRangeOfMouse()) return;

        Vector3Int pos = ActionManager.MouseTilePosition;

        if (OverNpc(ActionManager.MouseWorldPosition, out Npc npc))
        {
            _selectedNPC = npc;
            _destructableFound = DestructableType.Npc;
            _currentBreakTargetPosition = pos;
        }
        else if (ObjectManager.Instance.TryToFindWorldObject((Vector2Int)pos, out WorldObject wo) && wo.CanBeDestroyed)
        {
            _worldObjectSelected = wo;
            _destructableFound = DestructableType.WorldObject;
            _currentBreakTargetPosition = pos;
        }
        else if (FocusingOnWall)
        {
            Vector3Int wallPos = Pointer.IsOverTopTile() ? pos + Vector3Int.down : pos;
            if (TileRenderManager.Instance.WallTm.HasTile(wallPos))
            {
                _tileSelected = TileRenderManager.Instance.OreTm.HasTile(wallPos)
                    ? GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.OreTm.GetTile(wallPos))
                    : GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.WallTm.GetTile(wallPos));

                _destructableFound = DestructableType.Tile;
                _currentBreakTargetPosition = wallPos;
            }
        }
        else if (TileRenderManager.Instance.FloorTm.HasTile(pos))
        {
            _tileSelected = GameManager.Instance.GetTileSOFromTileBase(TileRenderManager.Instance.FloorTm.GetTile(pos));
            _destructableFound = DestructableType.Tile;
            _currentBreakTargetPosition = pos;
        }
    }

    private void HandleMiningLogic(MiningSpellItemSO miningSpellItemSO, int miningSpellSlotIndex)
    {
        bool wasMining = IsMining;

        if (_destructableFound == DestructableType.None || _breakCooldownTimer.RemainingSeconds > 0)
        {
            IsMining = false;
        }
        else if (SpellManager.Instance.CastTimeTimer.RemainingSeconds <= 0 && !SpellManager.Instance.IsContinuouslyCasting && SpellManager.Instance.IsSpellKeyHeld(miningSpellSlotIndex))
        {
            if (!IsMining)
            {
                BeginMining(miningSpellItemSO);
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

    private void BeginMining(MiningSpellItemSO miningSpellItemSO)
    {
        IsMining = true;
        _originalBreakTargetPosition = _currentBreakTargetPosition;

        float hardness = _destructableFound switch
        {
            DestructableType.WorldObject => _worldObjectSelected.Hardness,
            DestructableType.Tile => _tileSelected.Hardness,
            DestructableType.Npc => 1f,
            _ => 1f
        };

        float totalTicks = hardness * 30f / Mathf.Max(miningSpellItemSO.MiningPower, 0.1f);
        float totalMiningTime = totalTicks * 0.05f;
        _cachedTotalMiningTime = totalMiningTime;

        if (totalMiningTime == 0)
        {
            DestroyResource(null, null);
        }
        else
        {
            _miningTimer = new Timer(totalMiningTime);
            PlayMiningSound(null, null);
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

    private bool OverNpc(Vector2 position, out Npc nonPlayerCharacter)
    {
        Vector2 positionCheck = new(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        var colliders = Physics2D.OverlapBoxAll(positionCheck + new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 0);
        Npc npcToReturn = null;
        foreach (Collider2D col in colliders)
        {
            if (col.TryGetComponent(out Npc npc) && npc.GetComponent<NpcNetworkComponent>().NpcBiomeType == Player.LocalClientInstance.CurrentPlayerBiome.Value && col.CompareTag("FriendlyNpc"))
            {
                nonPlayerCharacter = npc;
                return true;
            }
        }
        nonPlayerCharacter = npcToReturn;
        return false;
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
                break;
            case DestructableType.Tile:
                TileRenderManager.Instance.DestroyTileServerRpc((Vector2Int)_currentBreakTargetPosition, GameManager.Instance.GetTileIdFromTileSO(_tileSelected), Player.LocalClientInstance.CurrentPlayerBiome.Value);
                break;
            case DestructableType.Npc:
                _selectedNPC.GetComponent<NpcNetworkComponent>().KillNpcServerRpc();
                break;
        }

        _breakCooldownTimer.RemainingSeconds = BreakCooldownDuration;
    }

    private void PlayMiningSound(object sender, EventArgs e)
    {
        _miningSoundTimer.RemainingSeconds = TimeBetweenMiningSounds;

        switch (_destructableFound)
        {
            case DestructableType.WorldObject:
                SoundManager.Instance.PlayOneShot(_worldObjectSelected.MiningSound, (Vector3)_currentBreakTargetPosition);
                break;
            case DestructableType.Tile:
                SoundManager.Instance.PlayOneShot(_tileSelected.MiningSound, (Vector3)_currentBreakTargetPosition);
                break;
            case DestructableType.Npc:
                SoundManager.Instance.PlayOneShot(_selectedNPC.DamageSound, (Vector3)_currentBreakTargetPosition);
                break;
        }
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnMiningFocusToggled -= ToggleTileFocus;
    }
}
