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
	public static Vector2 MouseWorldPosition { get; private set; }
	public static bool IsChargingSpell { get; private set; }
	
	public event EventHandler<OnStatUpdatedEventArgs> OnPlayerManaUpdated;
	public event EventHandler<OnStatUpdatedEventArgs> OnPlayerSpellChargeUpdated;
	public event EventHandler<OnStatUpdatedEventArgs> OnPlayerManaRechargeUpdated;
	public class OnStatUpdatedEventArgs : EventArgs
	{
		public float CurrentAmount, MaxAmount;
	}
	
	public Dictionary<ulong, Wand> WandDict { get; private set; } = new(); // Holds all wand data in your inventory

	private Timer _primaryActionTimer, _secondaryActionTimer;
	private InventoryItem _selectedInvItem;

	private void Awake()
	{
		Instance = this;

		_primaryActionTimer = new Timer(0.25f);
		_secondaryActionTimer = new Timer(0.25f);
	}

	private void Start()
	{
		InventoryManager.Instance.GetInventoryModel().OnWandCollected += OnWandCollected;
		InventoryManager.Instance.GetInventoryModel().OnWandRemoved += OnWandRemoved;
		InventoryManager.Instance.OnInventoryUpdated += UpdateWandDict;
		HotbarManager.Instance.OnFocusSlotUpdated += UpdateSelectedWand;
	}

    private void UpdateSelectedWand(object sender, HotbarManager.OnFocusItemSetEventArgs e)
    {
		InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem);
		
		if(_selectedInvItem != selectedInventoryItem)
		{
			foreach (ulong wandId in WandDict.Keys)
			{
				WandDict[wandId].SetSelected(wandId == selectedInventoryItem.Id);
			}
		}

		_selectedInvItem = selectedInventoryItem;

	}

    private void UpdateWandDict(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
	{
		HashSet<ulong> currentWandIds = new HashSet<ulong>();

		// Add missing wands from inventory to WandDict
		foreach (InventoryItem item in e.InventoryItems)
		{
			if (item is WandInventoryItem wandInventoryItem)
			{
				ulong wandId = wandInventoryItem.Id;
				currentWandIds.Add(wandId);

				if (!WandDict.ContainsKey(wandId))
				{
					AddWandToDict(wandInventoryItem);
				}
			}
		}

		// Remove wands from WandDict that are no longer in inventory
		var wandsToRemove = WandDict.Keys.Where(id => !currentWandIds.Contains(id)).ToList();
		foreach (var id in wandsToRemove)
		{
			WandDict[id].WandInvItem.ClearWandContentsUpdatedListeners();
			WandDict.Remove(id);
		}
	}

	private void OnWandCollected(object sender, InventoryModel.WandEventArgs e)
	{
		AddWandToDict(e.WandInvItem);
	}
	
	private void AddWandToDict(WandInventoryItem wandInventoryItem)
	{
		if (!WandDict.ContainsKey(wandInventoryItem.Id))
		{
			WandDict.Add(wandInventoryItem.Id, new Wand(wandInventoryItem));
		}
	}

	private void OnWandRemoved(object sender, InventoryModel.WandEventArgs e)
	{
		if(WandDict.ContainsKey(e.WandInvItem.Id))
		{
			WandDict[e.WandInvItem.Id].WandInvItem.ClearWandContentsUpdatedListeners();
			WandDict.Remove(e.WandInvItem.Id);
		}
	}

	private void Update()
	{
		if(Player.LocalClientInstance == null) return;
		
		MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		
		TickTimers(Time.deltaTime);
		TickWands(Time.deltaTime);
		HandleWandUI();
		HandleItemActionExecutions();
	}
	
	private void HandleItemActionExecutions()
	{
		if(Player.LocalClientInstance.IsDead() || Pointer.IsOverUI() || !GameInput.Instance.GetInputsEnabled()) return;

		if (GameInput.Instance.GetPrimaryHeldDown() && InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem))
		{
			if(_primaryActionTimer.RemainingSeconds <= 0)
			{
				_primaryActionTimer.RemainingSeconds = selectedInventoryItem.Item.ExecuteItemAction(selectedInventoryItem, Player.LocalClientInstance.MainHand);
			}
			
			if (WandDict.ContainsKey(selectedInventoryItem.Id) && !Player.LocalClientInstance.MainHand.IsSwinging && !GameInput.Instance.GetSecondaryHeldDown())
			{
				// Player is holding down primary on a wand, try to shoot wand
				WandDict[selectedInventoryItem.Id].CastSpell();
			}
		}
	}

	private void TickWands(float deltaTile)
	{
		if(WandDict.Count > 0)
		{
			foreach (var wand in WandDict)
			{
				wand.Value.Tick(deltaTile);
			}
		}
	}

	private void TickTimers(float deltaTime)
	{
		_primaryActionTimer.Tick(deltaTime);
		_secondaryActionTimer.Tick(deltaTime);
	}
	
	private void HandleWandUI()
	{
		if(InventoryManager.Instance.SelectedItemExists(out InventoryItem selectedInventoryItem) && selectedInventoryItem is WandInventoryItem wandInvItem)
		{
			// Update the UI stats for this wand
			AddWandToDict(wandInvItem);

			float currentMana = WandDict[wandInvItem.Id].CurrentMana;
			int maxMana = WandDict[wandInvItem.Id].WandSO.MaxMana;
			
			if(currentMana <= maxMana)
			{
				OnPlayerManaUpdated?.Invoke(this, new OnStatUpdatedEventArgs
				{
					MaxAmount = maxMana,
					CurrentAmount = currentMana
				});
			}
			
			float currentRecharge = WandDict[wandInvItem.Id].CurrentReload;
			float MaxRecharge = WandDict[wandInvItem.Id].TotalReloadDuration;
			
			if(currentRecharge <= MaxRecharge)
			{
				OnPlayerManaRechargeUpdated?.Invoke(this, new OnStatUpdatedEventArgs
				{
					MaxAmount = MaxRecharge,
					CurrentAmount = currentRecharge
				});
			}

			float totalCastTimeDuration = WandDict[wandInvItem.Id].CastTimeTimer.Duration;
			float currentCastTime = totalCastTimeDuration - WandDict[wandInvItem.Id].CastTimeTimer.RemainingSeconds;

			if (currentCastTime <= totalCastTimeDuration)
			{
				OnPlayerSpellChargeUpdated?.Invoke(this, new OnStatUpdatedEventArgs
				{
					MaxAmount = totalCastTimeDuration,
					CurrentAmount = currentCastTime
				});
			}
			
			IsChargingSpell = currentCastTime < totalCastTimeDuration;
		}
	}
	
	private void OnDestroy()
	{
		InventoryManager.Instance.GetInventoryModel().OnWandCollected -= OnWandCollected;
		InventoryManager.Instance.GetInventoryModel().OnWandRemoved -= OnWandRemoved;
		InventoryManager.Instance.OnInventoryUpdated -= UpdateWandDict;
		HotbarManager.Instance.OnFocusSlotUpdated -= UpdateSelectedWand;
	}
}
