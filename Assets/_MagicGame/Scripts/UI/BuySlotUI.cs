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
            // Money Logic
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
