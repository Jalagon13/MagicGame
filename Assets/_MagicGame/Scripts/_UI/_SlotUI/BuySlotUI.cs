using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectTinker
{
    public class BuySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [field: SerializeField] public Image ItemImage { get; private set; }

        private ItemDataSO _itemToBuy;
        private bool _hovered;

        public void Initialize(ItemDataSO itemToBuy)
        {
            _itemToBuy = itemToBuy;
            if (_hovered)
            {
                // This is here just so it stops the yellow message 
            }

            ItemImage.color = new Vector4(1, 1, 1, 1);
            ItemImage.sprite = itemToBuy.UiDisplay;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!GoldManager.Instance.CanAfford(_itemToBuy.GoldValue)) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                if (InventoryManager.Instance.GetMouseItem().MouseInventoryItem.HasItem)
                {
                    if (InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item.InGameName == _itemToBuy.InGameName)
                    {
                        InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity++;
                        SoundManager.Instance.PlayOneShot(FMODEvents.Instance.GoldPickup, Player.Instance.transform.position);
                        GoldManager.Instance.RemoveGold(_itemToBuy.GoldValue);
                        InventoryManager.Instance.GetInventoryModel().UpdateInventory();
                    }
                }
                else
                {
                    InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _itemToBuy;
                    InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity++;
                    SoundManager.Instance.PlayOneShot(FMODEvents.Instance.GoldPickup, Player.Instance.transform.position);
                    GoldManager.Instance.RemoveGold(_itemToBuy.GoldValue);
                    InventoryManager.Instance.GetInventoryModel().UpdateInventory();
                }
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!InventoryManager.MOUSE_HAS_ITEM)
            {
                _hovered = true;

                Tooltip.ShowNew();

                int quantity = 1;
                string quantityString = quantity > 1 ? $"[{quantity}]" : string.Empty;
                string itemText = $"{_itemToBuy.InGameName} {quantityString}<br>Cost: {_itemToBuy.GoldValue} Gold<br>{_itemToBuy.GetDescription()}";

                Tooltip.JustText(itemText, Color.white, fontSize: 12f);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Tooltip.HideUI();
        }
    }
}
