using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [field: SerializeField] public Image ItemImage { get; private set; }

    private ItemSO _itemToBuy;

    public void Initialize(ItemSO itemToBuy)
    {
        _itemToBuy = itemToBuy;
    
        ItemImage.color = new Vector4(1, 1, 1, 1);
        ItemImage.sprite = itemToBuy.UiDisplay;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.HasItem)
            {
                if(InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item.Name == _itemToBuy.Name)
                {
                    InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity++;
                    SoundManager.Instance.PlayOneShot(FMODEvents.Instance.GoldPickup, Player.LocalClientInstance.transform.position);
                    GoldManager.Instance.RemoveGold(_itemToBuy.GoldValue);
                    InventoryManager.Instance.GetInventoryModel().UpdateInventory();
                }
            }
            else if(GoldManager.Instance.CanAfford(_itemToBuy.GoldValue))
            {
                InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Item = _itemToBuy;
                InventoryManager.Instance.GetMouseItem().MouseInventoryItem.Quantity++;
                SoundManager.Instance.PlayOneShot(FMODEvents.Instance.GoldPickup, Player.LocalClientInstance.transform.position);
                GoldManager.Instance.RemoveGold(_itemToBuy.GoldValue);
                InventoryManager.Instance.GetInventoryModel().UpdateInventory();
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Tooltip stuff
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Tooltip.HideUI();
    }
}
