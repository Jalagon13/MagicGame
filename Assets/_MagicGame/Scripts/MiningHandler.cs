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
    public static bool FocusingOnWall = true;
    
    [field: SerializeField] public float TimeBetweenMiningSounds { get; private set; } = 0.25f;
    [field: SerializeField] public float DelayBetweenPlacingAndMining { get; private set; } = 0.15f;
    [field: SerializeField] public EventReference FocusWallSound { get; private set; }
    [field: SerializeField] public EventReference FocusFloorSound { get; private set; }

    private Timer _miningTimer, _miningSoundTimer;
    private DestructableType _destructableFound;
    private WorldObject _worldObjectSelected;
    private TileSO _tileSelected;
    private Vector3Int? _currentBreakTargetPosition = null;
    private StaffItemSO _staffItem;
    private bool _isMiningFlag;
    private bool _placeDelayActive;

    private void Awake()
    {
        _miningTimer = new Timer(0f);
        _miningSoundTimer = new Timer(TimeBetweenMiningSounds);
        _miningSoundTimer.OnTimerEnd += PlayMiningSound;
    }

    private void Start()
    {
        GameInput.Instance.OnSecondaryActionStarted += ToggleTileFocus;
    }

    private void ToggleTileFocus(object sender, EventArgs e)
    {
        bool selectedItemIsStaff = InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedItem) && selectedItem.Item is StaffItemSO;
        var mouseInventoryItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem;
        
        if(!selectedItemIsStaff || Pointer.IsOverUI() || Pointer.IsOverInteractable() || (mouseInventoryItem.HasItem && mouseInventoryItem.Item is StaffItemSO)) return;
    
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
        if (Player.LocalClientInstance == null) return;

        if (Player.LocalClientInstance.HealthState.IsDead || Pointer.IsOverUI() || !GameInput.Instance.GetInputsEnabled() || !InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem)) return;

        if(DeployItemSO.PlacedThisFrameFlag)
        {
            if(!_placeDelayActive)
            {
                StartCoroutine(SmallPlacementDelay());
            }
            
            return;
        }

        if(selectedInventoryItem.Item is StaffItemSO staffItem)
        {
            _staffItem = staffItem;
            _destructableFound = DestructableType.None;
            _currentBreakTargetPosition = null;

            if (staffItem.PlayerWithinMiningRangeOfMouse())
            {
                // Try to detect a destructable type to destroy
                if (ObjectManager.Instance.TryToFindWorldObject((Vector2Int)ActionManager.MouseTilePosition, out WorldObject wo) && wo.CanBeDestroyed)
                {
                    // Found an object to destroy
                    _worldObjectSelected = wo;
                    _destructableFound = DestructableType.WorldObject;
                    _currentBreakTargetPosition = ActionManager.MouseTilePosition;
                }
                else if (FocusingOnWall)
                {
                    // Try to find a destructable tile
                    if (TileManager.Instance.WallTm.HasTile(ActionManager.MouseTilePosition))
                    {
                        if(TileManager.Instance.OreTm.HasTile(ActionManager.MouseTilePosition))
                        {
                            _tileSelected = GameManager.Instance.GetTileSOFromTileBase(TileManager.Instance.OreTm.GetTile(ActionManager.MouseTilePosition));
                        }
                        else
                        {
                            _tileSelected = GameManager.Instance.GetTileSOFromTileBase(TileManager.Instance.WallTm.GetTile(ActionManager.MouseTilePosition));
                        }

                        _destructableFound = DestructableType.Tile;
                        _currentBreakTargetPosition = ActionManager.MouseTilePosition;
                    }
                }
                else if (TileManager.Instance.FloorTm.HasTile(ActionManager.MouseTilePosition))
                {
                    _tileSelected = GameManager.Instance.GetTileSOFromTileBase(TileManager.Instance.FloorTm.GetTile(ActionManager.MouseTilePosition));
                    _destructableFound = DestructableType.Tile;
                    _currentBreakTargetPosition = ActionManager.MouseTilePosition;
                }
            }

            if (_destructableFound == DestructableType.None)
            {
                _isMiningFlag = false;
                return;
            }

            if (GameInput.Instance.GetPrimaryHeldDown())
            {
                if (_currentBreakTargetPosition != ActionManager.MouseTilePosition || _isMiningFlag == false)
                {
                    _isMiningFlag = true;
                    _currentBreakTargetPosition = ActionManager.MouseTilePosition;

                    float hardness = _destructableFound switch
                    {
                        DestructableType.WorldObject => _worldObjectSelected.Hardness,
                        DestructableType.Tile => _tileSelected.Hardness,
                        _ => 1f // Default value
                    };

                    float totalTicks = hardness * 30f / Mathf.Max(_staffItem.MiningPower, 0.1f);
                    float totalMiningTime = totalTicks * 0.05f;
                    Debug.Log($"Total Mining Time: {totalMiningTime}");
                    if (totalMiningTime == 0)
                    {
                        DestroyResource(null, null);
                    }
                    else
                    {
                        _miningTimer = new Timer(totalMiningTime);
                        _miningTimer.OnTimerEnd -= DestroyResource;
                        _miningTimer.OnTimerEnd += DestroyResource;
                    }

                }

                if (_isMiningFlag)
                {
                    _miningTimer.Tick(Time.deltaTime);
                    _miningSoundTimer.Tick(Time.deltaTime);
                }
            }
            else
            {
                _isMiningFlag = false;
            }
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
                ObjectManager.Instance.DestroyObjectServerRpc(Player.LocalClientInstance.CurrentPlayerBiome.Value, (Vector2Int)ActionManager.MouseTilePosition, GameManager.Instance.GetIDFromWorldObject(_worldObjectSelected));
                break;
            case DestructableType.Tile:
                TileManager.Instance.DestroyTileServerRpc((Vector2Int)ActionManager.MouseTilePosition, GameManager.Instance.GetTileIdFromTileSO(_tileSelected), Player.LocalClientInstance.CurrentPlayerBiome.Value);
                break;
        }
    }

    private void PlayMiningSound(object sender, EventArgs e)
    {
        Instantiate(_staffItem.MiningVisualsPrefab, ActionManager.MouseWorldPosition, Quaternion.identity);
        _miningSoundTimer.RemainingSeconds = TimeBetweenMiningSounds;

        switch (_destructableFound)
        {
            case DestructableType.WorldObject:
                SoundManager.Instance.PlayOneShot(_worldObjectSelected.MiningSound, transform.position);
                break;
            case DestructableType.Tile:
                SoundManager.Instance.PlayOneShot(_tileSelected.MiningSound, transform.position);
                break;
        }
    }

    private void OnDestroy()
    {
        GameInput.Instance.OnSecondaryActionStarted -= ToggleTileFocus;
    }
}
