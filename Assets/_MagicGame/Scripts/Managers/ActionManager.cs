using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class ActionManager : MonoBehaviour
{
	public static ActionManager Instance { get; private set; }
	public static Vector2 MouseWorldPosition;
	
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
		HandleItemActionExecutions();
	}

	private bool CanPerformUpdate()
	{
		return Player.LocalClientInstance != null && !Player.LocalClientInstance.IsDead() && !Pointer.IsOverUI();
	}

	private void UpdateTimers(float deltaTime)
	{
		_primaryActionTimer.Tick(deltaTime);
		_secondaryActionTimer.Tick(deltaTime);
	}

	private void HandleItemActionExecutions()
	{
		if (GameInput.Instance.GetPrimaryHeldDown() && _primaryActionTimer.RemainingSeconds <= 0 && InventoryManager.Instance.MainHandItemExists(out InventoryItem mainHandInventoryItem))
		{
			_primaryActionTimer.RemainingSeconds = mainHandInventoryItem.Item.ExecuteItemAction(mainHandInventoryItem, Player.LocalClientInstance.MainHand);
		}
		
		if (GameInput.Instance.GetSecondaryHeldDown() && _secondaryActionTimer.RemainingSeconds <= 0 && InventoryManager.Instance.OffHandItemExists(out InventoryItem offHandInventoryItem))
		{
			_secondaryActionTimer.RemainingSeconds = offHandInventoryItem.Item.ExecuteItemAction(offHandInventoryItem, Player.LocalClientInstance.OffHand);
		}
	}

	private void HotbarManager_OnFocusItemSet(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
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
