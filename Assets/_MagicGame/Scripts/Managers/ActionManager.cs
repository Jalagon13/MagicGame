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
	
	private Timer _primaryActionTimer, _secondaryActionTimer, _spellBookTimer;
	
	private void Awake()
	{
		Instance = this;
		
		_primaryActionTimer = new Timer(0.25f);
		_secondaryActionTimer = new Timer(0.25f);
		_spellBookTimer = new Timer(0.25f);
	}
	
	private void Start()
	{
		HotbarManager.Instance.OnFocusSlotUpdated += TryToResetWand;
	}

	private void Update()
	{
		MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		
		if (!CanPerformUpdate()) return;

		UpdateTimers(Time.deltaTime);
		HandleItemActionExecutions();
	}
	
	private void TryToResetWand(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		var mainHandItemSO = GameManager.Instance.GetItemSOFromIndex(e.MainHandItemIndex);
		
		if(mainHandItemSO is SpellBookItemSO)
		{
			if(e.FocusItemSlotIndex == -1)
			{
				(InventoryManager.Instance.GetMouseItem().MouseInventoryItem as SpellBookInventoryItem).ResetSpellBook();
			}
			else
			{
				(InventoryManager.Instance.GetInventoryModel().InventoryItems[e.FocusItemSlotIndex] as SpellBookInventoryItem).ResetSpellBook();
			}
			
			_spellBookTimer.RemainingSeconds = 0;
		}
	}

	private bool CanPerformUpdate()
	{
		return Player.LocalClientInstance != null && !Player.LocalClientInstance.IsDead() && !Pointer.IsOverUI();
	}

	private void UpdateTimers(float deltaTime)
	{
		_primaryActionTimer.Tick(deltaTime);
		_secondaryActionTimer.Tick(deltaTime);
		_spellBookTimer.Tick(deltaTime);
	}

	private void HandleItemActionExecutions()
	{
		if (GameInput.Instance.GetPrimaryHeldDown() && InventoryManager.Instance.MainHandItemExists(out InventoryItem mainHandInventoryItem))
		{
			if(mainHandInventoryItem is SpellBookInventoryItem && _spellBookTimer.RemainingSeconds <= 0)
			{
				_spellBookTimer.RemainingSeconds = mainHandInventoryItem.Item.ExecuteItemAction(mainHandInventoryItem, Player.LocalClientInstance.MainHand);
			}
			else if(mainHandInventoryItem is not SpellBookInventoryItem && _primaryActionTimer.RemainingSeconds <= 0)
			{
				_primaryActionTimer.RemainingSeconds = mainHandInventoryItem.Item.ExecuteItemAction(mainHandInventoryItem, Player.LocalClientInstance.MainHand);
			}
		}
		
		if (GameInput.Instance.GetSecondaryHeldDown() && _secondaryActionTimer.RemainingSeconds <= 0 && InventoryManager.Instance.OffHandItemExists(out InventoryItem offHandInventoryItem))
		{
			_secondaryActionTimer.RemainingSeconds = offHandInventoryItem.Item.ExecuteItemAction(offHandInventoryItem, Player.LocalClientInstance.OffHand);
		}
	}
	
	private void OnDestroy()
	{
		HotbarManager.Instance.OnFocusSlotUpdated -= TryToResetWand;
	}
}
