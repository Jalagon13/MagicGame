

namespace AdvancedTooltips.Core
{
	using System;
	using System.Text;
	using AdvancedTooltips.ContentTypesHandlers;
	using TMPro;
	using UnityEngine;
	using UnityEngine.UI;

	public static class Tooltip
	{
		//* In this script should be placed functions that will be called form other scripts

		internal static TooltipsInstantiateHandler _instantiateHandler;
		internal static TooltipReferenceHolder _referenceHolder;


		public static void HideUI()
		{
			_referenceHolder.HideUI();
		}
		public static void ShowUI()
		{
			_referenceHolder.ShowUI();
		}
		public static void ShowNew()
		{
			ClearOldPrefabs();
			ShowUI();
			ReturnBackgroundToDefault();
			_referenceHolder.Layout.padding = _referenceHolder.defaultPadding;
		}
		public static void ReturnBackgroundToDefault()
		{
			_referenceHolder.background.sprite = _referenceHolder.defaultBackgroundSprite;
			_referenceHolder.background.color = _referenceHolder.defaultBackgroundColor;

		}
		public static void ClearOldPrefabs()
		{
			_referenceHolder.ClearOldPrefabs();
		}
		public static void CustomizeBackground(Sprite sprite, Color color)
		{
			_referenceHolder.background.sprite = sprite;
			_referenceHolder.background.color = color;
		}


		#region  just text
		/// <summary>
		///  if font == null -> will use default font
		/// </summary>
		public static void JustText(Sprite icon, Color colorOfIcon, string text, Color colorOfTheText, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
		{
			JustTextHandler script = _instantiateHandler.InstantiateJustText(customLayout);
			script.icon.sprite = icon;
			script.icon.color = colorOfIcon;

			script.text.font = font == null ? _referenceHolder.DefaultFont : font;
			script.text.text = text;
			script.text.color = colorOfTheText;
			script.text.fontSize = fontSize;
		}
		
		public static void JustText(Sprite icon, Color colorOfIcon, string text, Color colorOfTheText, float iconScale, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
		{
			JustTextHandler script = _instantiateHandler.InstantiateJustText(customLayout);
			script.icon.sprite = icon;
			script.icon.color = colorOfIcon;

			script.text.font = font == null ? _referenceHolder.DefaultFont : font;
			script.text.text = text;
			script.text.color = colorOfTheText;
			script.text.fontSize = fontSize;
			script.icon.transform.localScale = Vector3.one * iconScale;
		}
		
		public static void JustText(string text, Color colorOfTheText, TMP_FontAsset font = null, float fontSize = 20, Transform customLayout = null)
		{
			JustText(icon: null, new(0, 0, 0, 0), text, colorOfTheText, font: font, fontSize: fontSize, customLayout: customLayout);
		}
		#endregion

		public static void DisplayMaterial(MaterialsDisplay materialsDisplay, bool showPlusSignOnPositiveValues = true, bool showName = false, bool changeColorBasedOnAmount = true, Transform customLayout = null, TMP_FontAsset font = null, float fontSize = 20)
		{
			JustTextHandler script = _instantiateHandler.InstantiateJustText(customLayout);
			script.icon.sprite = materialsDisplay.icon;
			script.icon.color = Color.white;

			script.text.font = font == null ? _referenceHolder.DefaultFont : font;
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
			BuildingDisplayHandler script = _instantiateHandler.InstantiateBuildingDisplay(customLayout);
			script.icon.sprite = building.icon;

			script.name.font = font == null ? _referenceHolder.DefaultFont : font;
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
		
		public static void WandDisplay(SpellBookItemSO wand, SpellItemSO[] spellArray, Transform customLayout = null, float fontSize = 10)
		{
			WandTooltipDisplayHandlerUI script = _instantiateHandler.InstantiateWandTooltipDisplay(customLayout);
			script.WandName.text = wand.Name;
			script.WandIcon.sprite = wand.UiDisplay;
			
			JustText($"<br>Value: {wand.GoldValue} Gold"
			+ $"<br>{wand.Capacity}   capacity", Color.white, fontSize: fontSize, customLayout: script.StatLayout);
			
			for (int i = 0; i < spellArray.Length; i++)
			{
				SpellbookInventorySlotUI wandSlot = _instantiateHandler.InstantiateWandInvSlotUI(customLayout: script.MagicLayout);
				
				if(spellArray[i] != null)
				{
					wandSlot.SpellIcon.sprite = spellArray[i].SpellUIDisplaySprite;
				}
				else
				{
					wandSlot.SpellIcon.sprite = null;
					wandSlot.SpellIcon.color = Vector4.zero;
				}
			}
		}
		
		public static void SpellDisplay(SpellItemSO spell, Transform customLayout = null, float fontSize = 10)
		{
			WandTooltipDisplayHandlerUI script = _instantiateHandler.InstantiateWandTooltipDisplay(customLayout);
			script.WandName.text = spell.Name;
			script.WandIcon.sprite = spell.SpellUIDisplaySprite;

			JustText($"<br>Value: {spell.GoldValue} Gold<br>" + spell.GetDescription(), Color.white, fontSize: fontSize, customLayout: script.StatLayout);
			JustText($"{spell.Cooldown} s   cast delay"
			+ $"<br>{spell.Damage}   damage"
			+ $"<br>{spell.ManaCost}   cost to cast"
			+ $"<br>{spell.Knockback}   knockback"
			+ $"<br>{spell.Speed}   speed", Color.white, fontSize: fontSize, customLayout: script.StatLayout);
		}
		
		public static void CraftingRecipeDisplay(RecipeSO recipeSO, Transform customLayout = null, float fontSize = 10, float iconScale = 1)
		{
			JustText($"{recipeSO.OutputItem.Name} Recipe for ({recipeSO.OutputAmount}):<br>", Color.white, fontSize: fontSize);

			//for each ingredient in the recipe resource list
			foreach (InventoryItem ingredient in recipeSO.ResourceList)
			{
				JustTextHandler script = _instantiateHandler.InstantiateIngredientUI(customLayout);
				script.icon.sprite = ingredient.Item.UiDisplay;
				script.icon.transform.localScale = Vector3.one * iconScale;

				script.text.fontSize = fontSize;
				script.text.text = InventoryManager.Instance.GetInventoryModel().GetAmount(ingredient.Item) >= ingredient.Quantity ?
				$"{ingredient.Item.Name} ({ingredient.Quantity})<br>" :
				$"<color=red>{ingredient.Item.Name} ({ingredient.Quantity})</color><br>";
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