using System;
using Unity.Netcode;
using UnityEngine;

namespace ProjectWizard
{
    public class HotbarManager : MonoBehaviour
    {
        public event EventHandler<OnFocusItemSetEventArgs> OnFocusSlotUpdated;
        public class OnFocusItemSetEventArgs : EventArgs
        {
            // public InventoryItem FocusItem;
            public ushort SelectedItemId;
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

            var selectedItem = mouseHasItem ? InventoryManager.Instance.GetMouseItem().MouseInventoryItem : InventoryManager.Instance.GetInventoryModel().InventoryItems[GameInput.Instance.GetSelectedSlotIndex()];

            if (_focusInventoryItem == selectedItem) return;

            _focusInventoryItem = selectedItem;
            if (mouseHasItem)
            {
                InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetItemIdFromItemData(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item), -1);
            }
            else
            {
                InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetItemIdFromItemData(_focusInventoryItem.Item), GameInput.Instance.GetSelectedSlotIndex());
            }
        }

        private void InventoryManager_OnMouseItemUpdated(object sender, InventoryManager.InventoryItemEventArgs e)
        {
            if (e.InventoryItem.Item != null)
            {
                _focusInventoryItem = e.InventoryItem;
                InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetItemIdFromItemData(e.InventoryItem.Item), -1);
            }
            else
            {
                _focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[GameInput.Instance.GetSelectedSlotIndex()];
                InvokeOnFocusItemSetEvent(GameDataRegistry.INVALID_ID, GameInput.Instance.GetSelectedSlotIndex());
            }
        }

        private void GameInput_OnSlotSelected(object sender, GameInput.SlotSelectedEventArgs e)
        {
            if (InventoryManager.MOUSE_HAS_ITEM) return;

            _focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SelectedSlotIndex];

            if (_focusInventoryItem == null || _focusInventoryItem.Item == null)
            {
                InvokeOnFocusItemSetEvent(GameDataRegistry.INVALID_ID, e.SelectedSlotIndex);
            }
            else
            {
                InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetItemIdFromItemData(_focusInventoryItem.Item), e.SelectedSlotIndex);
            }
        }

        private void GameInput_OnScroll(object sender, GameInput.SlotSelectedEventArgs e)
        {
            if (InventoryManager.MOUSE_HAS_ITEM) return;

            _focusInventoryItem = InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SelectedSlotIndex];

            if (_focusInventoryItem == null || _focusInventoryItem.Item == null)
            {
                InvokeOnFocusItemSetEvent(GameDataRegistry.INVALID_ID, e.SelectedSlotIndex);
            }
            else
            {
                InvokeOnFocusItemSetEvent(GameDataRegistry.Instance.GetItemIdFromItemData(_focusInventoryItem.Item), e.SelectedSlotIndex);
            }
        }

        private void InvokeOnFocusItemSetEvent(ushort focusItemIndex, int selectedSlotIndex)
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
            return GameDataRegistry.Instance.GetItemIdFromItemData(_focusInventoryItem.Item);
        }

        private void OnDestroy()
        {
            GameInput.Instance.OnScroll -= GameInput_OnScroll;
            GameInput.Instance.OnSlotSelected -= GameInput_OnSlotSelected;
            InventoryManager.Instance.OnMouseItemUpdated -= InventoryManager_OnMouseItemUpdated;
            InventoryManager.Instance.OnInventoryUpdated -= InventoryManager_OnInventoryUpdated;
        }
    }
}
