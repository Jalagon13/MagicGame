namespace AdvancedTooltips.Core
{
    using AdvancedTooltips.ContentTypesHandlers;
    using TMPro;
    using UnityEngine;
    /// <summary>
    /// There should be only one of this script on scene
    /// </summary>
    [RequireComponent(typeof(TooltipReferenceHolder))]
    public class TooltipsInstantiateHandler : MonoBehaviour
    {

        //* As the name suggest, this script should be used for instantiating prefabs and configuring them.
        private TooltipReferenceHolder referenceHolder;

        private void Awake()
        {
            referenceHolder = GetComponent<TooltipReferenceHolder>();
            TooltipsStatic.instantiateHandler = this;
        }




        public JustTextHandler InstantiateJustText(Transform customLayout = null)
        {
            var gameObject = Instantiate(referenceHolder.JustTextPrefab, customLayout == null ? referenceHolder.layout.transform : customLayout);
            referenceHolder.oldPrefabs.Add(gameObject);

            return gameObject.GetComponent<JustTextHandler>();
        }

        public BuildingDisplayHandler InstantiateBuildingDisplay(Transform customLayout = null)
        {
            var gameObject = Instantiate(referenceHolder.buildingPrefab, customLayout == null ? referenceHolder.layout.transform : customLayout);
            referenceHolder.oldPrefabs.Add(gameObject);

            return gameObject.GetComponent<BuildingDisplayHandler>();
        }

    }
}