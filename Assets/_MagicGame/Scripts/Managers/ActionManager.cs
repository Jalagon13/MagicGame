using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class ActionManager : MonoBehaviour
{
	public static Vector2 MouseWorldPosition;

	public static ActionManager Instance { get; private set; }
	
	
	[SerializeField] private ItemParameter _clickDistanceParameter;
	
	private bool _mouseOverWall;
	private Timer _primaryActionTimer, _secondaryActionTimer, _miningCooldownTimer;
	private ItemSO _focusItemSO;
	private WandInventoryItem _wandItem;
	private float _primaryTimerDuration = 0.25f, _secondaryTimerDuration = 0.25f;
	private ResourceObject _selectedResourceObject;
	private float _actionRange = 1f;
	
	private void Awake()
	{
		Instance = this;
		
		_miningCooldownTimer = new Timer(0);
		_primaryActionTimer = new Timer(_primaryTimerDuration);
		_secondaryActionTimer = new Timer(_secondaryTimerDuration);
	}
	
	private void Start()
	{
		HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnFocusItemSet;
	}
	
	private void Update()
	{
		if(Player.LocalClientInstance == null || Player.LocalClientInstance.IsDead()) return;
	
		MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		
		_miningCooldownTimer.Tick(Time.deltaTime);
		_primaryActionTimer.Tick(Time.deltaTime);
		_secondaryActionTimer.Tick(Time.deltaTime);
		
		if(_focusItemSO == null || Pointer.IsOverUI()) return;
		
		if(_focusItemSO is WandItemSO && _miningCooldownTimer.RemainingSeconds <= 0 && PlayerInRangeOfMouse())
		{
			_wandItem = HotbarManager.Instance.GetFocusInventoryItem() as WandInventoryItem;
				
			if(GameInput.Instance.GetPrimaryHeldDown() || GameInput.Instance.GetSecondaryHeldDown())
			{
				// Spawn mining projectile
				bool mouseOverFloor = GetMouseOverFloor();
				bool mouseOverWall = GetMouseOverWall();
				bool resourceSelected = GetResourceSelected();

				if(!mouseOverFloor && !mouseOverWall && !resourceSelected) return;
					
				WandAttribute harvestType = GetHarvestType(mouseOverFloor, mouseOverWall, resourceSelected);
				AttributeData hitData = _wandItem.GetAttributeData(harvestType);

				GameManager.Instance.SpawnMiningProjectile(Player.LocalClientInstance.GetWandProjectileSpawnPoint().position, MouseWorldPosition, hitData.MiningPower, mouseOverFloor, mouseOverWall, resourceSelected);
				
				CalcMiningSpeed(harvestType);
				
				return;
			}
		}
		
		if(GameInput.Instance.GetPrimaryHeldDown() && _primaryActionTimer.RemainingSeconds <= 0 && !GameInput.Instance.GetSecondaryHeldDown())
		{
			_focusItemSO.ExecutePrimaryAction();
			_primaryActionTimer.RemainingSeconds = _primaryTimerDuration;
		}
		else if(GameInput.Instance.GetSecondaryHeldDown() && _secondaryActionTimer.RemainingSeconds <= 0 && !GameInput.Instance.GetPrimaryHeldDown())
		{
			_focusItemSO.ExecuteSecondaryAction();
			_secondaryActionTimer.RemainingSeconds = _secondaryTimerDuration;
		}
	}

	private WandAttribute GetHarvestType(bool mouseOverFloor, bool mouseOverWall, bool resourceSelected)
	{
		Vector3Int tilePosMouseIsHovering = Vector3Int.FloorToInt(MouseWorldPosition);
		Vector2Int tilePos = new (tilePosMouseIsHovering.x, tilePosMouseIsHovering.y);
		
		if(mouseOverFloor)
		{
			return Environment.Instance.GetFloorTilemapData().GetHarvestType(tilePos);
		}
		else if(mouseOverWall)
		{
			return Environment.Instance.GetWallTilemapData().GetHarvestType(tilePos);
		}
		else if(resourceSelected)
		{
			return _selectedResourceObject.GetHarvestType();
		}
		
		Debug.LogError($"Error, could not find a harvest type for mining");
		return default;
	}

	
	
	private void CalcMiningSpeed(WandAttribute wandAttribute)
	{
		AttributeData upgradeData = _wandItem.GetAttributeData(wandAttribute);
		float wandSpeedOfAttribute = upgradeData.MiningSpeed;
		float finalSpeed = wandSpeedOfAttribute / 60f;
		
		// Implement future buffs or speed prefex modifiers here.
		_miningCooldownTimer.RemainingSeconds = finalSpeed;
	}
	
	private bool GetMouseOverFloor()
	{
		Tilemap floorTilemap = Environment.Instance.GetFloorTilemapData().GetTilemap();
		Vector3Int tilePosition = Vector3Int.FloorToInt(MouseWorldPosition);
		
		return floorTilemap.HasTile(tilePosition);
	}
	
	private bool GetMouseOverWall()
	{
		Tilemap wallTilemap = Environment.Instance.GetWallTilemapData().GetTilemap();
		Vector3Int tilePosition = Vector3Int.FloorToInt(MouseWorldPosition);
		
		return wallTilemap.HasTile(tilePosition);
	}

	private bool GetResourceSelected()
	{
		Collider2D[] colliders = Physics2D.OverlapPointAll(MouseWorldPosition);
		List<ResourceObject> resourceObjectsFound = new();

		if (colliders.Count() > 0)
		{
			foreach (Collider2D c in colliders)
			{
				if (c.TryGetComponent(out ResourceObject resourceObject))
				{
					resourceObjectsFound.Add(resourceObject);
				}
			}
		}

		_selectedResourceObject = resourceObjectsFound.Count > 0 ? resourceObjectsFound.Last() : null;
		
		return _selectedResourceObject != null;
	}

	private void HotbarManager_OnFocusItemSet(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		if(e.FocusItemIndex <= -1) return;
		
		_focusItemSO = GameManager.Instance.GetItemSOFromIndex(e.FocusItemIndex);
		
		if(_focusItemSO != null)
		{
			float range = HotbarManager.Instance.GetFocusInventoryItem() is WandInventoryItem wandItem ? wandItem.GetRangeValue() : _focusItemSO.ExtractParameterValue(_clickDistanceParameter);
			_actionRange = range > 0 ? range : 1f;
		}
	}
	
	public bool PlayerInRangeOfMouse()
	{
		return Vector2.Distance(Player.LocalClientInstance.transform.position, MouseWorldPosition) <= _actionRange;
	}
	
	public float GetActionRange()
	{
		return _actionRange;
	}
	
	public bool MouseOverWall()
	{
		return _mouseOverWall;
	}
	
	private void OnDestroy()
	{
		HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnFocusItemSet;
	}
}
