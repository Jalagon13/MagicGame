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
	
	private float _actionRange = 3f;
	private Timer _primaryActionTimer, _secondaryActionTimer;
	private ItemSO _focusItemSO;
	
	private void Awake()
	{
		Instance = this;
		
		_primaryActionTimer = new Timer(0.25f);
		_secondaryActionTimer = new Timer(0.25f);
	}
	
	private void Start()
	{
		HotbarManager.Instance.OnFocusSlotUpdated += HotbarManager_OnFocusItemSet;
	}
	
	private void Update()
	{
		MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		
		if (!CanPerformUpdate()) return;

		UpdateTimers(Time.deltaTime);
		HandleItemActions();
	}

	private bool CanPerformUpdate()
	{
		return Player.LocalClientInstance != null && !Player.LocalClientInstance.IsDead() && _focusItemSO != null && !Pointer.IsOverUI();
	}

	private void UpdateTimers(float deltaTime)
	{
		_primaryActionTimer.Tick(deltaTime);
		_secondaryActionTimer.Tick(deltaTime);
	}

	private void HandleItemActions()
	{
		if (GameInput.Instance.GetPrimaryHeldDown() && _primaryActionTimer.RemainingSeconds <= 0 && !GameInput.Instance.GetSecondaryHeldDown())
		{
			_primaryActionTimer.RemainingSeconds = _focusItemSO.ExecuteItemAction(HotbarManager.Instance.GetFocusInventoryItem());
		}
		else if (GameInput.Instance.GetSecondaryHeldDown() && _secondaryActionTimer.RemainingSeconds <= 0 && !GameInput.Instance.GetPrimaryHeldDown())
		{
			_secondaryActionTimer.RemainingSeconds = _focusItemSO.ExecuteItemAction(HotbarManager.Instance.GetFocusInventoryItem());
		}
	}

	private void HotbarManager_OnFocusItemSet(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		if(e.FocusItemSlotIndex != -1)
		{
			_focusItemSO = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.FocusItemSlotIndex].Item;
		}
		else
		{
			_focusItemSO = InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item;
		}
		
		
		if(_focusItemSO != null)
		{
			float range = HotbarManager.Instance.GetFocusInventoryItem() is WandInventoryItem wandItem ? wandItem.GetRangeValue() : _focusItemSO.ExtractParameterValue(GameManager.Instance.GetItemParameterDataBaseSO().ClickDistanceParmeter);
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
	
	private void OnDestroy()
	{
		HotbarManager.Instance.OnFocusSlotUpdated -= HotbarManager_OnFocusItemSet;
	}
}
