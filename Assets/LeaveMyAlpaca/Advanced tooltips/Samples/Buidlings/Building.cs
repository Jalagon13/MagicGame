namespace AdvancedTooltips
{
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Building")]
    public class Building : ScriptableObject
    {
        public Sprite icon;
        public List<AdvancedTooltips.Core.TooltipsStatic.MaterialsDisplay> production;
        public List<AdvancedTooltips.Core.TooltipsStatic.MaterialsDisplay> constructionCosts;
    }
}