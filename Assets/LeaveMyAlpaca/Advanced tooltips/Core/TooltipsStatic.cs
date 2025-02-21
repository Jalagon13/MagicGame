

namespace AdvancedTooltips.Core
{
    using System;
    using AdvancedTooltips.ContentTypesHandlers;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public static class TooltipsStatic
    {
        //* In this script should be placed functions that will be called form other scripts

        internal static TooltipsInstantiateHandler instantiateHandler;
        internal static TooltipReferenceHolder referenceHolder;


        public static void HideUI()
        {
            referenceHolder.HideUI();
        }
        public static void ShowUI()
        {
            referenceHolder.ShowUI();
        }
        public static void ShowNew()
        {
            ClearOldPrefabs();
            ShowUI();
            ReturnBackgroundToDefault();
            referenceHolder.layout.padding = referenceHolder.defaultPadding;
        }
        public static void ReturnBackgroundToDefault()
        {
            referenceHolder.background.sprite = referenceHolder.defaultBackgroundSprite;
            referenceHolder.background.color = referenceHolder.defaultBackgroundColor;

        }
        public static void ClearOldPrefabs()
        {
            referenceHolder.ClearOldPrefabs();
        }
        public static void CustomizeBackground(Sprite sprite, Color color)
        {
            referenceHolder.background.sprite = sprite;
            referenceHolder.background.color = color;
        }


        #region  just text
        /// <summary>
        ///  if font == null -> will use default font
        /// </summary>
        public static void JustText(Sprite icon, Color colorOfIcon, string text, Color colorOfTheText, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
        {


            JustTextHandler script = instantiateHandler.InstantiateJustText(customLayout);
            script.icon.sprite = icon;
            script.icon.color = colorOfIcon;

            script.text.font = font == null ? referenceHolder.defaultFont : font;
            script.text.text = text;
            script.text.color = colorOfTheText;
            script.text.fontSize = fontSize;


        }
        public static void JustText(Sprite icon, Color colorOfIcon, string text, Color colorOfTheText, float iconScale, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
        {


            JustTextHandler script = instantiateHandler.InstantiateJustText(customLayout);
            script.icon.sprite = icon;
            script.icon.color = colorOfIcon;

            script.text.font = font == null ? referenceHolder.defaultFont : font;
            script.text.text = text;
            script.text.color = colorOfTheText;
            script.text.fontSize = fontSize;
            script.icon.transform.localScale = Vector3.one * iconScale;

        }
        public static void JustText(string text, Color colorOfTheText, TMP_FontAsset font = null, float fontSize = 20)
        {
            JustText(icon: null, new(0, 0, 0, 0), text, colorOfTheText, font: font, fontSize: fontSize);
        }
        #endregion

        public static void DisplayMaterial(MaterialsDisplay materialsDisplay, bool showPlusSignOnPositiveValues = true, bool showName = false, bool changeColorBasedOnAmount = true, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
        {
            JustTextHandler script = instantiateHandler.InstantiateJustText(customLayout);
            script.icon.sprite = materialsDisplay.icon;
            script.icon.color = Color.white;

            script.text.font = font == null ? referenceHolder.defaultFont : font;
            string sign = showPlusSignOnPositiveValues && materialsDisplay.amount > 0 ? "+" : "";
            script.text.text = $"{sign}{ExponentialNotation(materialsDisplay.amount)} {(showName ? materialsDisplay.name : "")}";

            if (!changeColorBasedOnAmount)
                script.text.color = Color.white;
            else
                script.text.color = materialsDisplay.amount > 0 ? Color.green : Color.red;

            script.text.fontSize = fontSize;
        }

        public static void BuildingDisplay(Building building, Transform customLayout = null, TMP_FontAsset font = null, float nameSize = 20, float fontSize = 10)
        {


            BuildingDisplayHandler script = instantiateHandler.InstantiateBuildingDisplay(customLayout);
            script.icon.sprite = building.icon;

            script.name.font = font == null ? referenceHolder.defaultFont : font;
            script.name.text = building.name;
            script.name.fontSize = nameSize;
            foreach (var materialsDisplay in building.production)
            {
                DisplayMaterial(materialsDisplay, showPlusSignOnPositiveValues: true, showName: true, customLayout: script.productionLayout, fontSize: fontSize);
            }
            foreach (var materialsDisplay in building.constructionCosts)
            {
                DisplayMaterial(materialsDisplay, showPlusSignOnPositiveValues: false, showName: true, customLayout: script.constructionCostsLayout, fontSize: fontSize);
            }
        }






        public static string ExponentialNotation(float amount)
        {

            float RoundedAmount;
            float DewidedBy10Nr;
            switch (amount)
            {
                case < 1000:
                    return new string(amount.ToString());
                case >= 1000 and < 1000000:
                    RoundedAmount = Mathf.Floor(amount / 100);
                    DewidedBy10Nr = RoundedAmount / 10;
                    return new string(DewidedBy10Nr + "K");
                case >= 1000000:
                    RoundedAmount = Mathf.Floor(amount / 100000);
                    DewidedBy10Nr = RoundedAmount / 10;
                    return new string(DewidedBy10Nr + "M");
                default:
                    return new string(amount.ToString());
            }
        }



        [Serializable]
        public class MaterialsDisplay
        {
            public string name;
            public int amount;
            public Sprite icon;
        }
    }
}