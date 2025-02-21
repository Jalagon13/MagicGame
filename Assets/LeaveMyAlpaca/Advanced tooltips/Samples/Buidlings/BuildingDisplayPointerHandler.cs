namespace AdvancedTooltips.Samples
{
    using Core;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    [AddComponentMenu("AdvancedTooltips/Samples/Building")]
    public class BuildingDisplayPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        public Building building;
        [SerializeField] private float fontSize = 20;
        [Tooltip("if empty will be using default font"), SerializeField] private TMP_FontAsset font;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (building == null)
                return;
            TooltipsStatic.ShowNew();


            TooltipsStatic.BuildingDisplay(building, customLayout: /* use default one */ null, font, fontSize: fontSize, nameSize: fontSize + 5);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipsStatic.HideUI();
        }
    }
}