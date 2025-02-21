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
			Tooltip._instantiateHandler = this;
		}




		public JustTextHandler InstantiateJustText(Transform customLayout = null)
		{
			var gameObject = Instantiate(referenceHolder.JustTextPrefab, customLayout == null ? referenceHolder.Layout.transform : customLayout);
			referenceHolder.oldPrefabs.Add(gameObject);

			return gameObject.GetComponent<JustTextHandler>();
		}

		public BuildingDisplayHandler InstantiateBuildingDisplay(Transform customLayout = null)
		{
			var gameObject = Instantiate(referenceHolder.BuildingPrefab, customLayout == null ? referenceHolder.Layout.transform : customLayout);
			referenceHolder.oldPrefabs.Add(gameObject);

			return gameObject.GetComponent<BuildingDisplayHandler>();
		}
		
		public WandTooltipDisplayHandlerUI InstantiateWandTooltipDisplay(Transform customLayout = null)
		{
			var gameObject = Instantiate(referenceHolder.WandPrefab, customLayout == null ? referenceHolder.Layout.transform : customLayout);
			referenceHolder.oldPrefabs.Add(gameObject);

			return gameObject.GetComponent<WandTooltipDisplayHandlerUI>();
		}
		
		public WandInventorySlotUI InstantiateWandInvSlotUI(Transform customLayout = null)
		{
			var gameObject = Instantiate(referenceHolder.WandInvSlotPrefab, customLayout == null ? referenceHolder.Layout.transform : customLayout);
			referenceHolder.oldPrefabs.Add(gameObject);

			return gameObject.GetComponent<WandInventorySlotUI>();
		}
	}
}