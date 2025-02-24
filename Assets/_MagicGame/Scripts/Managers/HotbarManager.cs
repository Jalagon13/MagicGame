using System;
using Unity.Netcode;
using UnityEngine;

public class HotbarManager : MonoBehaviour
{
	public event EventHandler<OnFocusItemSetEventArgs> OnFocusSlotUpdated;
	public class OnFocusItemSetEventArgs : EventArgs
	{
		// public InventoryItem FocusItem;
		public int MainHandItemIndex;
		public int FocusItemSlotIndex;
	}

	public static HotbarManager Instance { get; private set; }
	
	private InventoryItem _focusInventoryItem;

	private void Awake()
	{
		Instance = this;
	}
	
	private void Start()
	{
		GameInput.Instance.OnScroll += GameInput_OnScroll;
		GameInput.Instance.OnSlotSelected += GameInput_OnSlotSelected;	
		InventoryManager.Instance.OnMouseItemUpdated += InventoryManager_OnMouseItemUpdated;
		InventoryManager.Instance.OnInventoryUpdated += InventoryManager_OnInventoryUpdated;
	}

	private void InventoryManager_OnInventoryUpdated(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
	{
		bool mouseHasItem = InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item != null;
		
		_focusInventoryItem = mouseHasItem ? InventoryManager.Instance.GetMouseItem().MouseInventoryItem : InventoryManager.Instance.GetInventoryModel().InventoryItems[GameInput.Instance.GetSelectedSlotIndex()];
		if(mouseHasItem)
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIdFromItemSO(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item), -1);
		}
		else
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIdFromItemSO(_focusInventoryItem.Item), GameInput.Instance.GetSelectedSlotIndex());
		}
	}

	private void InventoryManager_OnMouseItemUpdated(object sender, InventoryManager.InventoryItemEventArgs e)
	{
		if(e.InventoryItem.Item != null)
		{
			_focusInventoryItem = e.InventoryItem;
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIdFromItemSO(e.InventoryItem.Item), -1);
		}
		else
		{
			_focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[GameInput.Instance.GetSelectedSlotIndex()]; 
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIdFromItemSO(_focusInventoryItem.Item), GameInput.Instance.GetSelectedSlotIndex());
		}
	}

	private void GameInput_OnSlotSelected(object sender, GameInput.SlotSelectedEventArgs e)
	{
		_focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SelectedSlotIndex];
		
		if(_focusInventoryItem == null)
		{
			InvokeOnFocusItemSetEvent(-1, e.SelectedSlotIndex);
		}
		else
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIdFromItemSO(_focusInventoryItem.Item), e.SelectedSlotIndex);
		}
	}

	private void GameInput_OnScroll(object sender, GameInput.SlotSelectedEventArgs e)
	{
		_focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SelectedSlotIndex];
		
		if(_focusInventoryItem == null)
		{
			InvokeOnFocusItemSetEvent(-1, e.SelectedSlotIndex);
		}
		else
		{
			InvokeOnFocusItemSetEvent(GameManager.Instance.GetItemIdFromItemSO(_focusInventoryItem.Item), e.SelectedSlotIndex);
		}
	}
	
	private void InvokeOnFocusItemSetEvent(int focusItemIndex, int selectedSlotIndex)
	{
		OnFocusSlotUpdated?.Invoke(this, new OnFocusItemSetEventArgs
		{
			MainHandItemIndex = focusItemIndex,
			FocusItemSlotIndex = selectedSlotIndex
		});
	}
	
	public InventoryItem GetFocusInventoryItem()
	{
		return _focusInventoryItem;
	}
	
	public int GetFocusItemIndex()
	{
		return GameManager.Instance.GetItemIdFromItemSO(_focusInventoryItem.Item);
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnScroll -= GameInput_OnScroll;
		GameInput.Instance.OnSlotSelected -= GameInput_OnSlotSelected;	
		InventoryManager.Instance.OnMouseItemUpdated -= InventoryManager_OnMouseItemUpdated;
		InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
	}
}
