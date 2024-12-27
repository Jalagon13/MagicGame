using System;
using Unity.Netcode;
using UnityEngine;

public class HotbarManager : MonoBehaviour
{
	public event EventHandler<OnFocusItemSetEventArgs> OnFocusSlotUpdated;
	public class OnFocusItemSetEventArgs : EventArgs
	{
		// public InventoryItem FocusItem;
		public int FocusItemIndex;
		public int FocusItemSlotIndex;
	}

	public static HotbarManager Instance { get; private set; }
	
	private InventoryItem _focusInventoryItem;
	private int _selectedSlotIndex;

	private void Awake()
	{
		Instance = this;
		Debug.Log("hotbar");
	}
	
	private void Start()
	{
		GameInput.Instance.OnScroll += GameInput_OnScroll;
		GameInput.Instance.OnSlotSelected += GameInput_OnSlotSelected;	
		InventoryManager.Instance.OnMouseItemUpdated += InventoryManager_OnMouseItemUpdated;
	}

	private void InventoryManager_OnMouseItemUpdated(object sender, InventoryManager.OnMouseItemUpdatedEventArgs e)
	{
		_focusInventoryItem = e.MouseItem;
		
		if(e.MouseItem.Item != null)
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIndexFromItemObject(e.MouseItem.Item), -1);
		}
		else
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIndexFromItemObject(_focusInventoryItem.Item), _selectedSlotIndex);
		}
	}

	private void GameInput_OnSlotSelected(object sender, GameInput.SlotSelectedEventArgs e)
	{
		_focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SelectedSlotIndex];
		_selectedSlotIndex = e.SelectedSlotIndex;
		
		if(_focusInventoryItem == null)
		{
			InvokeOnFocusItemSetEvent(-1, e.SelectedSlotIndex);
		}
		else
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIndexFromItemObject(_focusInventoryItem.Item), e.SelectedSlotIndex);
		}
	}

	private void GameInput_OnScroll(object sender, GameInput.SlotSelectedEventArgs e)
	{
		_focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SelectedSlotIndex];
		_selectedSlotIndex = e.SelectedSlotIndex;
		
		if(_focusInventoryItem == null)
		{
			InvokeOnFocusItemSetEvent(-1, e.SelectedSlotIndex);
		}
		else
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIndexFromItemObject(_focusInventoryItem.Item), e.SelectedSlotIndex);
		}
	}
	
	private void InvokeOnFocusItemSetEvent(int focusItemIndex, int selectedSlotIndex)
	{
		OnFocusSlotUpdated?.Invoke(this, new OnFocusItemSetEventArgs
		{
			FocusItemIndex = focusItemIndex,
			FocusItemSlotIndex = selectedSlotIndex
		});
	}
	
	public InventoryItem GetFocusInventoryItem()
	{
		return _focusInventoryItem;
	}
	
	public int GetFocusItemIndex()
	{
		return GameManager.Instance.GetItemIndexFromItemObject(_focusInventoryItem.Item);
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnScroll -= GameInput_OnScroll;
		GameInput.Instance.OnSlotSelected -= GameInput_OnSlotSelected;	
		InventoryManager.Instance.OnMouseItemUpdated -= InventoryManager_OnMouseItemUpdated;
	}
}
