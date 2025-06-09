using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : NetworkBehaviour
{
	public static GameInput Instance { get; private set; }

	public event EventHandler OnShiftStarted;
	public event EventHandler OnSpaceStarted;
	public event EventHandler OnSpaceCanceled;
	public event EventHandler OnFKeyPressed;
	public event EventHandler OnSecondaryActionStarted;
	public event EventHandler<OnPrimaryOrSecondaryActionEventArgs> OnPrimaryAction;
	public event EventHandler<OnPrimaryOrSecondaryActionEventArgs> OnSecondaryAction;
	public class OnPrimaryOrSecondaryActionEventArgs : EventArgs
	{
		public bool IsHeldDown;
	}
	
	public event EventHandler<SlotSelectedEventArgs> OnScroll;
	public event EventHandler<SlotSelectedEventArgs> OnSlotSelected;
	public class SlotSelectedEventArgs : EventArgs
	{
		public int SelectedSlotIndex;
		public InputAction.CallbackContext Context;
	}
	public event EventHandler<InputAction.CallbackContext> OnMove;
	public event EventHandler<OnToggleInventoryEventArgs> OnInventoryToggle;
	public class OnToggleInventoryEventArgs : EventArgs
	{
		public bool InventoryOpen;
	}
	
	private PlayerInput _playerInput;
	private bool _inventoryOpen, _primaryHeldDown, _secondaryHeldDown, _shiftHeldDown, _spaceHeldDown, _inputsEnabled = true;
	private int _selectedSlotIndex = 0;
	
	private void Awake()
	{
		Instance = this;
		
		_playerInput = new();
		_playerInput.Enable();
		_playerInput.Player.Move.started += PlayerInput_OnMove;
		_playerInput.Player.Move.performed += PlayerInput_OnMove;
		_playerInput.Player.Move.canceled += PlayerInput_OnMove;
		_playerInput.Player.ToggleInventory.started += PlayerInput_OnToggleInventory;
		_playerInput.Hotbar.Scroll.performed += PlayerInput_OnScroll;
		_playerInput.Hotbar._1.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._2.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._3.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._4.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._5.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._6.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._7.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._8.started += PlayerInput_SlotSelected;
		_playerInput.Hotbar._9.started += PlayerInput_SlotSelected;
		_playerInput.Player.PrimaryAction.started += PlayerInput_PrimaryAction; 
		_playerInput.Player.PrimaryAction.performed += PlayerInput_PrimaryAction; 
		_playerInput.Player.PrimaryAction.canceled += PlayerInput_PrimaryAction; 
		_playerInput.Player.SecondaryAction.started += PlayerInput_SecondaryActionStarted; 
		_playerInput.Player.SecondaryAction.performed += PlayerInput_SecondaryAction; 
		_playerInput.Player.SecondaryAction.canceled += PlayerInput_SecondaryActionCanceled; 
		_playerInput.Player.SwapHands.started += PlayerInput_FKeyPressed;
		_playerInput.Player.Shift.started += PlayerInput_ShiftStart;
		_playerInput.Player.Shift.canceled += PlayerInput_ShiftCanceled;
		_playerInput.Player.Space.started += PlayerInput_SpaceStarted;
		_playerInput.Player.Space.canceled += PlayerInput_SpaceCanceled;
	}

    private void Start()
	{
		WorldManager.Instance.OnBiomeTransitionStart += WorldManager_DisableInputs;
		WorldManager.Instance.OnBiomeTransitionEnd += WorldManager_EnableInputs;
		InGameMenu.Instance.OnMenuOpen += InGameMenu_OnMenuOpen;
		Player.OnAnyPlayerSpawned += RegisterOnPlayerLifeStateChanged;
	}

    public override void OnDestroy()
	{
		_playerInput.Disable();
		_playerInput.Dispose();

		WorldManager.Instance.OnBiomeTransitionStart -= WorldManager_DisableInputs;
		WorldManager.Instance.OnBiomeTransitionEnd -= WorldManager_EnableInputs;
		InGameMenu.Instance.OnMenuOpen -= InGameMenu_OnMenuOpen;
		Player.OnAnyPlayerSpawned -= RegisterOnPlayerLifeStateChanged;
		
		if (Player.LocalClientInstance != null)
		{
			Player.LocalClientInstance.ServerCharacter.NetLifeState.LifeState.OnValueChanged -= OnPlayerLifeStateChanged;
		}
	}

    private void RegisterOnPlayerLifeStateChanged(object sender, Player.PlayerIdEventArgs e)
    {
		if (NetworkManager.LocalClientId != e.PlayerId) return;

		Player.LocalClientInstance.ServerCharacter.NetLifeState.LifeState.OnValueChanged += OnPlayerLifeStateChanged;
	}

	private void OnPlayerLifeStateChanged(LifeState previousValue, LifeState newValue)
	{
		if (previousValue == LifeState.Alive && newValue == LifeState.Dead)
		{
			Debug.Log($"Player died, inputs disabled");
			_inputsEnabled = false;
		}
		else if (previousValue == LifeState.Dead && newValue == LifeState.Alive)
		{
			Debug.Log($"Player respawned, inputs enabled");
			_inputsEnabled = true;
		}
	}

	private void InGameMenu_OnMenuOpen(object sender, EventArgs e)
    {
		_inventoryOpen = true;

		OnInventoryToggle?.Invoke(this, new OnToggleInventoryEventArgs
		{
			InventoryOpen = _inventoryOpen
		});
	}

    private void PlayerInput_SpaceStarted(InputAction.CallbackContext context)
	{
		OnSpaceStarted?.Invoke(this, EventArgs.Empty);
		_spaceHeldDown = true;
	}

	private void PlayerInput_SpaceCanceled(InputAction.CallbackContext context)
	{
		OnSpaceCanceled?.Invoke(this, EventArgs.Empty);
		_spaceHeldDown = false;
	}

	private void WorldManager_EnableInputs(object sender, EventArgs e)
	{
		_inputsEnabled = true;
	}

	private void WorldManager_DisableInputs(object sender, EventArgs e)
	{
		_inputsEnabled = false;
	}

	private void PlayerInput_ShiftStart(InputAction.CallbackContext context)
	{
		OnShiftStarted?.Invoke(this, EventArgs.Empty);
	
		_shiftHeldDown = true;
	}
	
	private void PlayerInput_ShiftCanceled(InputAction.CallbackContext context)
	{
		_shiftHeldDown = false;
	}

	private void PlayerInput_FKeyPressed(InputAction.CallbackContext context)
	{
		OnFKeyPressed?.Invoke(this, EventArgs.Empty);
	}

	private void PlayerInput_SecondaryActionStarted(InputAction.CallbackContext context)
	{
		if(!_inputsEnabled) return;
	
		OnSecondaryActionStarted?.Invoke(this, EventArgs.Empty);
	}

	private void PlayerInput_SecondaryAction(InputAction.CallbackContext context)
	{
		_secondaryHeldDown = context.performed;
	
		if(!_inputsEnabled) return;
	
		OnSecondaryAction?.Invoke(this, new OnPrimaryOrSecondaryActionEventArgs
		{
			IsHeldDown = _secondaryHeldDown
		});
	}
	
	private void PlayerInput_SecondaryActionCanceled(InputAction.CallbackContext context)
	{
		_secondaryHeldDown = context.performed;
	}

	private void PlayerInput_PrimaryAction(InputAction.CallbackContext context)
	{
		_primaryHeldDown = context.performed;
		
		if(!_inputsEnabled) return;
	
		OnPrimaryAction?.Invoke(this, new OnPrimaryOrSecondaryActionEventArgs
		{
			IsHeldDown = _primaryHeldDown
		});
	}

	private void PlayerInput_SlotSelected(InputAction.CallbackContext context)
	{
		_selectedSlotIndex = Int32.Parse(context.action.name) - 1;

		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.FocusSlotChanged, Player.LocalClientInstance.transform.position);

		OnSlotSelected?.Invoke(this, new SlotSelectedEventArgs
		{
			SelectedSlotIndex = _selectedSlotIndex,
			Context = context
		});
	}

	private void PlayerInput_OnScroll(InputAction.CallbackContext context)
	{
		if(Pointer.IsOverUI()) return;
		
		float scrollNum = context.ReadValue<float>();
		
		if (scrollNum < 0)
		{
			_selectedSlotIndex++;
			if (_selectedSlotIndex > InventoryManager.HOTBAR_SLOTS_AMOUNT)
				_selectedSlotIndex = 0;
		}
		else if(scrollNum > 0)
		{
			_selectedSlotIndex--;
			if(_selectedSlotIndex < 0)
				_selectedSlotIndex = InventoryManager.HOTBAR_SLOTS_AMOUNT;
		}
	
		OnScroll?.Invoke(this, new SlotSelectedEventArgs
		{
			SelectedSlotIndex = _selectedSlotIndex,
			Context = context
		});

		SoundManager.Instance.PlayOneShot(FMODEvents.Instance.FocusSlotChanged, Player.LocalClientInstance.transform.position);
	}
	
	private void PlayerInput_OnToggleInventory(InputAction.CallbackContext context)
	{
		_inventoryOpen = !_inventoryOpen;
		
		OnInventoryToggle?.Invoke(this, new OnToggleInventoryEventArgs
		{
			InventoryOpen = _inventoryOpen
		});
	}

	private void PlayerInput_OnMove(InputAction.CallbackContext context)
	{
		OnMove?.Invoke(this, context);
	}
	
	public bool GetPrimaryHeldDown()
	{
		return _primaryHeldDown;
	}

	public bool GetSecondaryHeldDown()
	{
		return _secondaryHeldDown;
	}

	public bool GetShiftHeldDown()
	{
		return _shiftHeldDown;
	}
	
	public bool GetSpaceHeldDown()
	{
	    return _spaceHeldDown;
	}
	
	public int GetSelectedSlotIndex()
	{
		return _selectedSlotIndex;
	}
	
	
	public bool GetInputsEnabled()
	{
		return _inputsEnabled;
	}
}
