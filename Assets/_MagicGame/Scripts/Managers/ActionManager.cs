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

	private Timer _primaryActionTimer, _secondaryActionTimer, _spellBookTimer;
	private float _currentMana, _maxMana, _manaChargeSpeed, _currentRecharge, _maxRecharge, _rechargeSpeed;

	private void Awake()
	{
		Instance = this;

		_primaryActionTimer = new Timer(0.25f);
		_secondaryActionTimer = new Timer(0.25f);
		_spellBookTimer = new Timer(0.25f);
	}

	private void Start()
	{
		HotbarManager.Instance.OnFocusSlotUpdated += TryToResetSpellBook;
	}

	private void Update()
	{
		MouseWorldPosition = (Vector2)Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

		if (!CanPerformUpdate()) return;

		UpdateTimers(Time.deltaTime);
		UpdateManaAndRecharge(Time.deltaTime);
		HandleItemActionExecutions();
	}

	private void UpdateManaAndRecharge(float deltaTime)
	{
		if (_currentMana < _maxMana)
		{
			SetCurrentMana(_currentMana + _manaChargeSpeed * deltaTime);
		}
		
		if(_currentRecharge < _maxRecharge)
		{
			SetCurrentRecharge(_currentRecharge + _rechargeSpeed * deltaTime);
		}
	}
	
	private void SetCurrentRecharge(float newRecharge)
	{
		float previousRecharge = _currentRecharge;
		_currentRecharge = Mathf.Clamp(newRecharge, 0, _maxMana);

		if (Mathf.Abs(_currentRecharge - previousRecharge) > Mathf.Epsilon)
		{
			OnPlayerManaRechargeUpdated?.Invoke(this, new OnStatUpdatedEventArgs
			{
				PreviousValue = Mathf.FloorToInt(previousRecharge),
				NewValue = Mathf.FloorToInt(_currentRecharge),
				MaxValue = Mathf.FloorToInt(_maxRecharge)
			});
		}
	}

	private void SetCurrentMana(float newMana)
	{
		float previousMana = _currentMana;
		_currentMana = Mathf.Clamp(newMana, 0, _maxMana);

		if (Mathf.Abs(_currentMana - previousMana) > Mathf.Epsilon)
		{
			OnPlayerManaUpdated?.Invoke(this, new OnStatUpdatedEventArgs
			{
				PreviousValue = Mathf.FloorToInt(previousMana),
				NewValue = Mathf.FloorToInt(_currentMana),
				MaxValue = Mathf.FloorToInt(_maxMana)
			});
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
			if (mainHandInventoryItem is SpellBookInventoryItem spellBookInventoryItem && _spellBookTimer.RemainingSeconds <= 0)
			{
				if (spellBookInventoryItem.HasSpells())
				{
					if (spellBookInventoryItem.GetCurrentSpell().ManaCost <= _currentMana)
					{
						SetCurrentMana(_currentMana - spellBookInventoryItem.GetCurrentSpell().ManaCost);
						
						if(spellBookInventoryItem.IsCurrentSpellFinalSpell())
						{
							_currentRecharge = 0;
						}

						_spellBookTimer.RemainingSeconds = mainHandInventoryItem.Item.ExecuteItemAction(mainHandInventoryItem, Player.LocalClientInstance.MainHand);
					}
					else
					{
						_spellBookTimer.RemainingSeconds = (spellBookInventoryItem.Item as SpellBookItemSO).RechargeTime;
						
						_currentRecharge = 0;
						
						spellBookInventoryItem.ResetSpellBook();
					}
				}
			}
			else if (mainHandInventoryItem is not SpellBookInventoryItem && _primaryActionTimer.RemainingSeconds <= 0)
			{
				_primaryActionTimer.RemainingSeconds = mainHandInventoryItem.Item.ExecuteItemAction(mainHandInventoryItem, Player.LocalClientInstance.MainHand);
			}
		}

		if (GameInput.Instance.GetSecondaryHeldDown() && _secondaryActionTimer.RemainingSeconds <= 0 && InventoryManager.Instance.OffHandItemExists(out InventoryItem offHandInventoryItem))
		{
			_secondaryActionTimer.RemainingSeconds = offHandInventoryItem.Item.ExecuteItemAction(offHandInventoryItem, Player.LocalClientInstance.OffHand);
		}
	}
	
	private void TryToResetSpellBook(object sender, HotbarManager.OnFocusItemSetEventArgs e)
	{
		var mainHandItemSO = GameManager.Instance.GetItemSOFromItemId(e.MainHandItemIndex);

		if (mainHandItemSO is SpellBookItemSO spellBookItemSO)
		{
			int maxMana = 0;
			int manaChargeSpeed = 0;

			if (e.FocusItemSlotIndex == -1)
			{
				(maxMana, manaChargeSpeed) = (InventoryManager.Instance.GetMouseItem().MouseInventoryItem as SpellBookInventoryItem).ResetSpellBook();
			}
			else
			{
				(maxMana, manaChargeSpeed) = (InventoryManager.Instance.GetInventoryModel().InventoryItems[e.FocusItemSlotIndex] as SpellBookInventoryItem).ResetSpellBook();
			}

			_spellBookTimer.RemainingSeconds = 0;
			_maxMana = maxMana;
			_manaChargeSpeed = manaChargeSpeed;
			SetCurrentMana(_maxMana);

			// Set recharge to maxMana and calculate recharge speed
			_maxRecharge = maxMana;
			_rechargeSpeed = _maxRecharge / spellBookItemSO.RechargeTime;  // Set recharge speed
			SetCurrentRecharge(_maxRecharge);
		}
	}

	private void OnDestroy()
	{
		HotbarManager.Instance.OnFocusSlotUpdated -= TryToResetSpellBook;
	}
}
