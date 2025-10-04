using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectWizard
{
    public class CraftNodeUI : MonoBehaviour
    {
        [SerializeField]
        private RecipeDataSO _recipeSO;

        [Header("Dependencies")]
        [SerializeField]
        private Image _outputImage;

        [SerializeField]
        private TextMeshProUGUI _outputAmountText;

        [SerializeField]
        private Button _craftButton;


        private bool _canCraft, _hovered;

        private void OnDisable()
        {
            if (_hovered)
            {
                Tooltip.HideUI();
            }
        }

        private void OnEnable()
        {
            UpdateCraftStatus();
        }

        private void Start()
        {
            Initialize(_recipeSO);

            InventoryManager.Instance.OnInventoryUpdated += UpdateCraftStatus;
        }

        private void OnDestroy()
        {
            InventoryManager.Instance.OnInventoryUpdated -= UpdateCraftStatus;
        }

        private void UpdateCraftStatus(object sender, InventoryManager.OnInventoryUpdatedEventArgs e)
        {
            UpdateCraftStatus();
        }

        public void Initialize(RecipeDataSO recipe)
        {
            if (recipe != null)
            {
                _recipeSO = recipe;
                _outputImage.sprite = _recipeSO.OutputItem.UiDisplay;
                _outputAmountText.text = _recipeSO.OutputAmount == 1 ? string.Empty : _recipeSO.OutputAmount.ToString();
            }

            UpdateCraftStatus();
        }

        // Attached to the InvItem button component
        public void OnCraft()
        {
            InventoryManager.Instance.TryToCraft(_recipeSO);
        }

        // Connected to Event Trigger component on _outputImage
        public void OnPointerEnter()
        {
            Tooltip.ShowNew();
            Tooltip.CraftingRecipeDisplay(_recipeSO, fontSize: 12f, iconScale: 0.6f);
            _hovered = true;
        }

        // Connected to Event Trigger component on _outputImage
        public void OnPointerExit()
        {
            // TooltipManager.Instance.Hide();
            Tooltip.HideUI();
            _hovered = false;
        }

        private void UpdateCraftStatus()
        {
            if (_recipeSO == null || InventoryManager.Instance == null) return;

            _canCraft = InventoryManager.Instance.HasAllIngredients(_recipeSO.ResourceList);

            // Edit CraftNodeView visuals depending on _canCraft
            _craftButton.interactable = _canCraft;
            // ...
        }
    }
}
