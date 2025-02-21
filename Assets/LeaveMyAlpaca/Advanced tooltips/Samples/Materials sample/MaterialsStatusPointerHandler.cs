namespace AdvancedTooltips.Samples
{
    using Core;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    [AddComponentMenu("AdvancedTooltips/Samples/Materials Status")]
    public class MaterialsStatusPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private MaterialType materialType;

        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Color backgroundColor;

        [SerializeField] private float nameSize = 25;
        [SerializeField] private float sizeOfRestOfTheText = 20;
        [Tooltip("if empty will be using default font"), SerializeField] private TMP_FontAsset font;

        public void OnPointerEnter(PointerEventData eventData)
        {
            Tooltip.ShowNew();
            Tooltip.CustomizeBackground(backgroundSprite, backgroundColor);


            Tooltip.JustText(materialType.name, Color.white, font, nameSize);

            Tooltip.JustText($"{Tooltip.ExponentialNotation(materialType.amountInStorage)} in storage", Color.white, font, sizeOfRestOfTheText);

            Color colorOfTheIncome = materialType.income <= 0 ? Color.red : Color.green;
            string incomeSign = materialType.income > 0 ? "+" : "";
            string incomeText = $"{incomeSign}{Tooltip.ExponentialNotation(materialType.income)} income";
            Tooltip.JustText(incomeText, colorOfTheIncome, font, sizeOfRestOfTheText);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Tooltip.HideUI();
        }
    }
}