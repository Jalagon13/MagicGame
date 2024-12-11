using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldInput : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerExitHandler, IPointerUpHandler
{
    public event EventHandler<PointerEventData> OnEnter;
    public event EventHandler<PointerEventData> OnDown;
    public event EventHandler<PointerEventData> OnExit;
    public event EventHandler<PointerEventData> OnUp;
    public event EventHandler<InputAction.CallbackContext> OnInteractStarted;
    public event EventHandler<InputAction.CallbackContext> OnInteractPerformed;
    public event EventHandler<InputAction.CallbackContext> OnInteractCanceled;
	
    private PlayerInput _playerInput;
    private bool _mouseOverDetector;
	
    private void Awake()
    {
        _playerInput = new();
        _playerInput.Player.Interact.started += InteractStarted;
        _playerInput.Player.Interact.performed += InteractPerformed;
        _playerInput.Player.Interact.canceled += InteractCanceled;
        _playerInput.Enable();
    }
	
    private void OnDestroy()
    {
        _playerInput.Disable();
    }
	
    public void InteractStarted(InputAction.CallbackContext context)
    {
        OnInteractStarted?.Invoke(this, context);
    }
	
    public void InteractPerformed(InputAction.CallbackContext context)
    {
        OnInteractPerformed?.Invoke(this, context);
    }
	
    public void InteractCanceled(InputAction.CallbackContext context)
    {
        OnInteractCanceled?.Invoke(this, context);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _mouseOverDetector = true;
        OnEnter?.Invoke(this, eventData);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDown?.Invoke(this, eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _mouseOverDetector = false;
        OnExit?.Invoke(this, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnUp?.Invoke(this, eventData);
    }
	
    public bool GetMouseOverDetector()
    {
        return _mouseOverDetector;
    }
}
