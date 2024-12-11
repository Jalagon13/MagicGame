using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler<OnPrimaryOrSecondaryActionEventArgs> OnSecondaryAction;
    public event EventHandler<OnPrimaryOrSecondaryActionEventArgs> OnPrimaryAction;
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
	
    private bool _inventoryOpen, _primaryHeldDown, _secondaryHeldDown;
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
        _playerInput.Player.SecondaryAction.started += PlayerInput_SecondaryAction; 
        _playerInput.Player.SecondaryAction.performed += PlayerInput_SecondaryAction; 
        _playerInput.Player.SecondaryAction.canceled += PlayerInput_SecondaryAction; 
    }
	
    private void Start()
    {
        InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
    }

    private void InventoryManager_OnInventoryUpdated(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
    {
        OnSlotSelected?.Invoke(this, new SlotSelectedEventArgs
        {
            SelectedSlotIndex = _selectedSlotIndex,
        });
    }

    private void PlayerInput_SecondaryAction(InputAction.CallbackContext context)
    {
        _secondaryHeldDown = context.performed;
	
        OnSecondaryAction?.Invoke(this, new OnPrimaryOrSecondaryActionEventArgs
        {
            IsHeldDown = _secondaryHeldDown
        });
    }

    private void PlayerInput_PrimaryAction(InputAction.CallbackContext context)
    {
        _primaryHeldDown = context.performed;
	
        OnPrimaryAction?.Invoke(this, new OnPrimaryOrSecondaryActionEventArgs
        {
            IsHeldDown = _primaryHeldDown
        });
    }

    private void PlayerInput_SlotSelected(InputAction.CallbackContext context)
    {
        _selectedSlotIndex = Int32.Parse(context.action.name) - 1;
	
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

    private void OnDestroy()
    {
        _playerInput.Disable();
        _playerInput.Dispose();	
		
        InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
    }
}
