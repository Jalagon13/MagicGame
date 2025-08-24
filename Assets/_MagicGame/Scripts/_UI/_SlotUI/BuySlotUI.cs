using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [field: SerializeField] public Image ItemImage { get; private set; }

    private ItemSO _itemToBuy;
    private bool _hovered;

    public void Initialize(ItemSO itemToBuy)
    {
        _itemToBuy = itemToBuy;
        if(_hovered)
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
            if(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.HasItem)
            {
                if(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item.Name == _itemToBuy.Name)
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

            switch (_itemToBuy)
            {
                case WandItemSO wandItemSO:
                    SpellItemSO[] magicArray = new SpellItemSO[0];
                    Tooltip.WandDisplay(wandItemSO, magicArray, fontSize: 12f);
                    break;
                case SpellItemSO spellItemSO:
                    Tooltip.SpellDisplay(spellItemSO, fontSize: 12f);
                    break;
                default:
                    int quantity = 1;
                    string quantityString = quantity > 1 ? $"[{quantity}]" : string.Empty;
                    string itemText = $"{_itemToBuy.Name} {quantityString}<br>Cost: {_itemToBuy.GoldValue} Gold<br>{_itemToBuy.GetDescription()}";

                    Tooltip.JustText(itemText, Color.white, fontSize: 12f);
                    break;
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        Tooltip.HideUI();
    }
}
