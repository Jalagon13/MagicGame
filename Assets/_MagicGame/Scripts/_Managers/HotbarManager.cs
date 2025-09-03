using System;
using Unity.Netcode;
using UnityEngine;

public class HotbarManager : MonoBehaviour
{
	public event EventHandler<OnFocusItemSetEventArgs> OnFocusSlotUpdated;
	public class OnFocusItemSetEventArgs : EventArgs
	{
		// public InventoryItem FocusItem;
		public int SelectedItemId;
		public int SelectedItemInventorySlotIndex;
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
			InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetUShortIdFromItemData(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item), -1);
		}
		else
		{
			InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetUShortIdFromItemData(_focusInventoryItem.Item), GameInput.Instance.GetSelectedSlotIndex());
		}
	}

	private void InventoryManager_OnMouseItemUpdated(object sender, InventoryManager.InventoryItemEventArgs e)
	{
		if(e.InventoryItem.Item != null)
		{
			_focusInventoryItem = e.InventoryItem;
			InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetUShortIdFromItemData(e.InventoryItem.Item), -1);
		}
		else
		{
			_focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[GameInput.Instance.GetSelectedSlotIndex()]; 
			InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetUShortIdFromItemData(_focusInventoryItem.Item), GameInput.Instance.GetSelectedSlotIndex());
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
			InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetUShortIdFromItemData(_focusInventoryItem.Item), e.SelectedSlotIndex);
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
			InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetUShortIdFromItemData(_focusInventoryItem.Item), e.SelectedSlotIndex);
		}
	}
	
	private void InvokeOnFocusItemSetEvent(int focusItemIndex, int selectedSlotIndex)
	{
		OnFocusSlotUpdated?.Invoke(this, new OnFocusItemSetEventArgs
		{
			SelectedItemId = focusItemIndex,
			SelectedItemInventorySlotIndex = selectedSlotIndex
		});
	}
	
	public InventoryItem GetFocusInventoryItem()
	{
		return _focusInventoryItem;
	}
	
	public int GetFocusItemIndex()
	{
		return GameDataRegistry.Instance.GetUShortIdFromItemData(_focusInventoryItem.Item);
	}
	
	private void OnDestroy()
	{
		GameInput.Instance.OnScroll -= GameInput_OnScroll;
		GameInput.Instance.OnSlotSelected -= GameInput_OnSlotSelected;	
		InventoryManager.Instance.OnMouseItemUpdated -= InventoryManager_OnMouseItemUpdated;
		InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
	}
}
