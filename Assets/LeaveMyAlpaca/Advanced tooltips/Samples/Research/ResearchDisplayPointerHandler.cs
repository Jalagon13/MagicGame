namespace AdvancedTooltips.Samples
{
    using System.Collections.Generic;
    using Core;
    using TMPro;
    using UnityEngine;
    using UnityEngine.EventSystems;
    [AddComponentMenu("AdvancedTooltips/Samples/Research")]
    public class ResearchDisplayPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public List<TooltipsStatic.MaterialsDisplay> costs = new();
        public List<Building> unlockedBuildings = new();



        [SerializeField] private float fontSize = 20;
        [Tooltip("if empty will be using default font"), SerializeField] private TMP_FontAsset font;

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipsStatic.ShowNew();

            if (costs.Count != 0)
            {
                TooltipsStatic.JustText("<b>Costs:", Color.white, fontSize: fontSize + 5);
                for (int i = 0; i < costs.Count; i++)
                {
                    TooltipsStatic.DisplayMaterial(costs[i], showPlusSignOnPositiveValues: true, showName: true, changeColorBasedOnAmount: true, fontSize: fontSize);
                }

            }


            if (unlockedBuildings.Count != 0)
            {
                TooltipsStatic.JustText("<b>Unlocked buildings:", Color.white, fontSize: fontSize + 5);
                for (int i = 0; i < unlockedBuildings.Count; i++)
                {

                    TooltipsStatic.BuildingDisplay(unlockedBuildings[i], customLayout: /* use default one */ null, font, fontSize: fontSize, nameSize: fontSize + 5);
                }
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipsStatic.HideUI();
        }
    }
}