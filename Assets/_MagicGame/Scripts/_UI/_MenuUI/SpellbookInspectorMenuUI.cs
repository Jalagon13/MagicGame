using System;
using System.Collections.Generic;
using AdvancedTooltips.Core;
using TMPro;
using UnityEngine;

namespace ProjectWizard
{
    public class SpellbookInspectorMenuUI : MonoBehaviour
    {
        public WandInventoryItem SelectedWand { get; private set; }

        [field: SerializeField] public TextMeshProUGUI InspectorTitleText;
        [SerializeField] private WandInventorySlotUI _wandInvSlotPrefab;
        [SerializeField] private Transform _spellBookSlotsHolder;
        [SerializeField] private WandSlotUI _spellBookSlotUI;

        private void OnEnable()
        {
            InventoryManager.Instance.OnInventorySlotShiftLeftClicked += OnInventorySlotShiftLeftClicked_SpellBookShortCut;
        }

        private void OnDisable()
        {
            InventoryManager.Instance.OnInventorySlotShiftLeftClicked -= OnInventorySlotShiftLeftClicked_SpellBookShortCut;

            if (HasWand())
            {
                InventoryManager.Instance.AddItem(SelectedWand);
            }

            RemoveSelectedWand();
            InventoryManager.Instance.GetInventoryModel().UpdateInventory();
        }

        private void OnInventorySlotShiftLeftClicked_SpellBookShortCut(object sender, InventoryManager.ShortCutInventoryItemEventArgs e)
        {
            if (e.InventoryItem is WandInventoryItem wandInInv)
            {
                if (HasWand())
                {
                    InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = SwapWands(wandInInv);
                    InventoryManager.Instance.ShowInventoryItemTooltip(InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex]);
                }
                else
                {
                    PlaceSelectedWand(wandInInv);
                    InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new();
                    Tooltip.HideUI();
                }
            }
            else if (e.InventoryItem.Item is SpellItemSO magicItemSO)
            {
                if (HasWand())
                {
                    WandInventorySlotUI firstEmptySpellBookInventorySlotUI = null;

                    foreach (Transform child in _spellBookSlotsHolder)
                    {
                        if (!child.GetComponent<WandInventorySlotUI>().WandInventorySlotIsOccupied())
                        {
                            firstEmptySpellBookInventorySlotUI = child.GetComponent<WandInventorySlotUI>();
                            break;
                        }
                    }

                    if (firstEmptySpellBookInventorySlotUI != null)
                    {
                        // Found an empty spot
                        firstEmptySpellBookInventorySlotUI.SetMagic(magicItemSO);
                        InventoryManager.Instance.GetInventoryModel().InventoryItems[e.SlotIndex] = new();
                        Tooltip.HideUI();
                    }
                }
            }

            _spellBookSlotUI.UpdateSlotUI();
            InventoryManager.Instance.GetInventoryModel().UpdateInventory();
        }

        public bool HasWand()
        {
            return SelectedWand != null;
        }

        public WandInventoryItem RemoveSelectedWand()
        {
            WandInventoryItem removedWand = SelectedWand;

            SelectedWand = null;

            RemoveUI();

            InGameMenu.Instance.ClearOldMenu();

            return removedWand;
        }

        public void PlaceSelectedWand(WandInventoryItem wandItem)
        {
            if (wandItem == null)
            {
                throw new ArgumentNullException(nameof(wandItem), "Cannot place a null wand.");
            }

            SelectedWand = wandItem;

            UpdateWandSlotsUI();
        }

        public WandInventoryItem SwapWands(WandInventoryItem newWand)
        {
            if (newWand == null)
            {
                throw new ArgumentNullException(nameof(newWand), "Cannot swap with a null spellbook.");
            }

            // Store the currently selected spellbook
            WandInventoryItem previousWand = SelectedWand;

            // Replace the selected spellbook with the new one
            SelectedWand = newWand;

            Debug.Log($"Swapped spellbook. New selected spellbook: {SelectedWand.Item.InGameName}");

            // Update the UI
            UpdateWandSlotsUI();

            // Return the previous spellbook
            return previousWand;
        }

        private void UpdateWandSlotsUI()
        {
            // Clear existing craft nodes
            RemoveUI();

            // Create new craft nodes based on the crafting model's recipe list
            if (SelectedWand != null)
            {
                for (int i = 0; i < SelectedWand.MagicArray.Length; i++)
                {
                    WandInventorySlotUI wandInvSlotUI = Instantiate(_wandInvSlotPrefab, _spellBookSlotsHolder);
                    wandInvSlotUI.Initialize(SelectedWand, i);
                }
            }

            InspectorTitleText.text = $"Inspecting {SelectedWand.Item.InGameName}";
            _spellBookSlotUI.UpdateSlotUI();
            InventoryManager.Instance.GetInventoryModel().UpdateInventory();
        }

        private void RemoveUI()
        {
            if (_spellBookSlotsHolder.childCount != 0)
            {
                foreach (Transform child in _spellBookSlotsHolder)
                {
                    Destroy(child.gameObject);
                }
            }

            InspectorTitleText.text = $"Spellbook Inspector";
        }
    }
}