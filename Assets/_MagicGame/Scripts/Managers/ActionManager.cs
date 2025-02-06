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
	public event EventHandler<OnStatUpdatedEventArgs> OnPlayerManaUpdated;
	public event EventHandler<OnStatUpdatedEventArgs> OnPlayerManaRechargeUpdated;
	public class OnStatUpdatedEventArgs : EventArgs
	{
		public int PreviousValue, NewValue, MaxValue;
	}

	private Timer _primaryActionTimer, _secondaryActionTimer;
	private Dictionary<ulong, Wand> _wandDict = new(); // Holds all wand data in your inventory

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
	}

	private void OnWandCollected(object sender, InventoryModel.WandEventArgs e)
	{
		if(!_wandDict.ContainsKey(e.WandId))
		{
			_wandDict.Add(e.WandId, new Wand(e.WandItemSO));
		}
		else
		{
			Debug.LogError($"Trying to add a wand that already exist. {e.WandItemSO.Name} ID: {e.WandId}");
		}
	}

	private void OnWandRemoved(object sender, InventoryModel.WandEventArgs e)
	{
		if(_wandDict.ContainsKey(e.WandId))
		{
			_wandDict.Remove(e.WandId);
		}
		else
		{
			Debug.LogError($"Trying to remove wand that does not exist. {e.WandItemSO.Name} ID: {e.WandId}");
		}
	}

	private void Update()
	{
		if(Player.LocalClientInstance == null) return;
		
		MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
		
		HandleItemActionExecutions();
		TickTimers(Time.deltaTime);
		TickWands(Time.deltaTime);
	}

	private void TickWands(float deltaTile)
	{
		if(_wandDict.Count > 0)
		{
			foreach (var wand in _wandDict)
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

	private void HandleItemActionExecutions()
	{
		if(Player.LocalClientInstance.IsDead() || Pointer.IsOverUI()) return;
	
		if (GameInput.Instance.GetPrimaryHeldDown() && _primaryActionTimer.RemainingSeconds <= 0 && InventoryManager.Instance.MainHandItemExists(out InventoryItem mainHandInventoryItem))
		{
			_primaryActionTimer.RemainingSeconds = mainHandInventoryItem.Item.ExecuteItemAction(mainHandInventoryItem, Player.LocalClientInstance.MainHand);
		}

		if (GameInput.Instance.GetSecondaryHeldDown() && _secondaryActionTimer.RemainingSeconds <= 0 && InventoryManager.Instance.OffHandItemExists(out InventoryItem offHandInventoryItem))
		{
			_secondaryActionTimer.RemainingSeconds = offHandInventoryItem.Item.ExecuteItemAction(offHandInventoryItem, Player.LocalClientInstance.OffHand);
		}
	}

	private void OnDestroy()
	{
		InventoryManager.Instance.GetInventoryModel().OnWandCollected -= OnWandCollected;
		InventoryManager.Instance.GetInventoryModel().OnWandRemoved -= OnWandRemoved;
	}
}
