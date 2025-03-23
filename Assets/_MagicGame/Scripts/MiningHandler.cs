using System;
using UnityEngine;

public enum DestructableType
{
    Tile,
    WorldObject,
    None
}

public class MiningHandler : MonoBehaviour
{
    private Timer _miningTimer;
    private bool _focusOnWall = true;
    private DestructableType _destructableFound;
    private WorldObject _worldObjectSelected;
    private TileSO _tileSelected;
    private Vector3Int? _currentBreakTargetPosition = null;
    private StaffItemSO _staffItem;
    private bool _isMiningFlag;

    private void Awake()
    {
        _miningTimer = new Timer(0f);
    }
    
    private void Start()
    {
        GameInput.Instance.OnSecondaryActionStarted += ToggleTileFocus;
    }

    private void ToggleTileFocus(object sender, EventArgs e)
    {
        _focusOnWall = !_focusOnWall;
        Debug.Log("Toggle Tile Focus, focusing on wall: " + _focusOnWall);
    }

    private void Update()
    {
        if (Player.LocalClientInstance == null) return;

        if (Player.LocalClientInstance.HealthState.IsDead || Pointer.IsOverUI() || !GameInput.Instance.GetInputsEnabled()) return;

        if (InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem) && selectedInventoryItem.Item is StaffItemSO staffItem)
        {
            _staffItem = staffItem;
        
            if (staffItem.PlayerWithinMiningRangeOfMouse())
            {
                // Detection Code
                _destructableFound = DestructableType.None;
                _currentBreakTargetPosition = null;

                // Try to detect a destructable type to destroy
                if (ObjectManager.Instance.TryToFindWorldObject((Vector2Int)ActionManager.MouseTilePosition, out WorldObject wo))
                {
                    // Found an object to destroy
                    // Debug.Log($"Object: {wo.gameObject.name} found at {ActionManager.MouseTilePosition}");
                    
                    _worldObjectSelected = wo;
                    _destructableFound = DestructableType.WorldObject;
                    _currentBreakTargetPosition = ActionManager.MouseTilePosition;
                }
                else if(_focusOnWall)
                {
                    // Try to find a destructable tile
                    if (TileManager.Instance.WallTm.HasTile(ActionManager.MouseTilePosition))
                    {
                        // Debug.Log($"Tile: {TileManager.Instance.WallTm.GetTile(ActionManager.MouseTilePosition).name} found at {ActionManager.MouseTilePosition}");
                        
                        _tileSelected = GameManager.Instance.GetTileSOFromTileBase(TileManager.Instance.WallTm.GetTile(ActionManager.MouseTilePosition));
                        _destructableFound = DestructableType.Tile;
                        _currentBreakTargetPosition = ActionManager.MouseTilePosition;
                    }
                }
                else if (TileManager.Instance.FloorTm.HasTile(ActionManager.MouseTilePosition))
                {
                    // Debug.Log($"Tile: {TileManager.Instance.FloorTm.GetTile(ActionManager.MouseTilePosition).name} found at {ActionManager.MouseTilePosition}");
                    
                    _tileSelected = GameManager.Instance.GetTileSOFromTileBase(TileManager.Instance.FloorTm.GetTile(ActionManager.MouseTilePosition));
                    _destructableFound = DestructableType.Tile;
                    _currentBreakTargetPosition = ActionManager.MouseTilePosition;
                }
            }
        }

        if (_destructableFound == DestructableType.None)
        {
            _isMiningFlag = false;
            return;
        }

        if (GameInput.Instance.GetPrimaryHeldDown())
        {
            if(_currentBreakTargetPosition != ActionManager.MouseTilePosition || _isMiningFlag == false)
            {
                Debug.Log($"Held down and moving to {ActionManager.MouseTilePosition}");
                
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
                _miningTimer = new Timer(totalMiningTime);
                _miningTimer.OnTimerEnd -= DestroyResource;
                _miningTimer.OnTimerEnd += DestroyResource;
            }
            
            if(_isMiningFlag)
            {
                _miningTimer.Tick(Time.deltaTime);
            }
        }
        else
        {
            _isMiningFlag = false;
        }
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

    private void OnDestroy()
    {
        GameInput.Instance.OnSecondaryActionStarted -= ToggleTileFocus;
    }
}
