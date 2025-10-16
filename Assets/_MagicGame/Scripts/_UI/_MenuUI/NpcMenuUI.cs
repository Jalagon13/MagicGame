using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTinker
{
    public class NpcMenuUI : MonoBehaviour
    {
        [field: SerializeField] public Transform BuySlotsTransform { get; private set; }
        [field: SerializeField] public Transform SellSlotsTransform { get; private set; }
        [field: SerializeField] public BuySlotUI BuySlotUIPrefab { get; private set; }
        [field: SerializeField] public TextMeshProUGUI TotalText { get; private set; }
        [field: SerializeField] public Button SellButton { get; private set; }

        private List<ItemDataSO> _itemsToSell;
        private List<InventoryItem> _sellItemInventory = new List<InventoryItem>();

        private void Start()
        {
            SellButton.onClick.AddListener(() => { SellItems(); });
            InventoryManager.Instance.OnInventorySlotClicked += InventoryManager_OnInventorySlotClicked;
        }

        private void OnDisable()
        {
            InventoryManager.Instance.OnInventorySlotClicked -= InventoryManager_OnInventorySlotClicked;
        }

        private void SellItems()
        {
            int totalSellValue = 0;

            for (int i = 0; i < _sellItemInventory.Count; i++)
            {
                if (_sellItemInventory[i].HasItem)
                {
                    totalSellValue += _sellItemInventory[i].Item.GoldValue * _sellItemInventory[i].Quantity;
                    _sellItemInventory[i] = new InventoryItem { Item = null, Quantity = 0 };
                }
            }

            if (totalSellValue > 0)
            {
                SoundManager.Instance.PlayOneShot(FMODEvents.Instance.GoldPickup, Player.Instance.transform.position);
                GoldManager.Instance.AddGold(totalSellValue);
            }

            UpdateSellSlots();
        }

        private void InventoryManager_OnInventorySlotClicked(object sender, EventArgs e)
        {
            UpdateSellSlots();
        }

        private void UpdateSellSlots()
        {
            int total = 0;

            foreach (Transform child in SellSlotsTransform)
            {
                int sellSlotIndex = child.GetSiblingIndex();

                child.GetComponent<InventorySlotUI>().InitializeInvSlotUI(sellSlotIndex, _sellItemInventory);
                child.GetComponent<InventorySlotUI>().UpdateDisplayUI(_sellItemInventory[sellSlotIndex]);

                if (_sellItemInventory[sellSlotIndex].HasItem)
                {
                    total += _sellItemInventory[sellSlotIndex].Item.GoldValue * _sellItemInventory[sellSlotIndex].Quantity;
                }
            }

            TotalText.text = $"Total Gold: {total}";
        }

        public void SetItemsToSell(List<ItemDataSO> itemsToSell)
        {
            _itemsToSell = itemsToSell;

            foreach (var item in itemsToSell)
            {
                var buySlot = Instantiate(BuySlotUIPrefab, BuySlotsTransform);
                buySlot.Initialize(item);
            }
        }

        public void InitializeSellSlots()
        {
            for (int i = 0; i < SellSlotsTransform.childCount; i++)
            {
                _sellItemInventory.Add(new InventoryItem() { Item = null, Quantity = 0 });
            }

            foreach (Transform child in SellSlotsTransform)
            {
                int chestSlotIndex = child.GetSiblingIndex();

                child.GetComponent<InventorySlotUI>().InitializeInvSlotUI(chestSlotIndex, _sellItemInventory);
                child.GetComponent<InventorySlotUI>().UpdateDisplayUI(new());
            }

            UpdateSellSlots();
        }
    }
}